using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hemma.Modules.Property.Gdpr;

public sealed partial class PropertyPersonalDataExporter(
    PropertyDbContext db,
    ILogger<PropertyPersonalDataExporter> logger) : IPersonalDataExporter
{
    public async Task<PersonalDataExport> ExportAsync(UserRef user, CancellationToken ct)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal);

        try
        {
            var assignedTasks = await db.Projects
                .AsNoTracking()
                .SelectMany(
                    project => project.Tasks,
                    (project, task) => new
                    {
                        project.Name,
                        project.HouseholdId,
                        task.AssigneeId,
                        task.Title,
                        task.Status,
                        task.DueDate,
                    })
                .Where(row => row.AssigneeId == user.UserId)
                .ToListAsync(ct);

            data["assignedTasks"] = assignedTasks.Select(row => new
            {
                taskTitle = row.Title,
                projectName = row.Name,
                householdId = row.HouseholdId,
                status = row.Status.ToString(),
                dueDate = row.DueDate,
            }).ToList();
        }
        catch (PostgresException ex) when (string.Equals(ex.SqlState, PostgresErrorCodes.UndefinedTable, StringComparison.Ordinal))
        {
            LogExportSkippedMissingTable(logger, user.UserId, ex);
        }

        return new PersonalDataExport(user.UserId, "Property", data);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Property personal-data export for user {UserId} skipped: a Property table is missing. If Property migrations have run on this host, this indicates a real deployment error and personal data was NOT exported.")]
    private static partial void LogExportSkippedMissingTable(ILogger logger, Guid userId, Exception exception);
}
