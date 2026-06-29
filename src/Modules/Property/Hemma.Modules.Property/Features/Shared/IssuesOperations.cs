using ErrorOr;
using Hemma.Modules.Notifications.Contracts.Dtos;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Features.AddAttachment;
using Hemma.Modules.Property.Features.AddLink;
using Hemma.Modules.Property.Features.AddTask;
using Hemma.Modules.Property.Features.ArchiveArea;
using Hemma.Modules.Property.Features.ArchiveTag;
using Hemma.Modules.Property.Features.AssignTags;
using Hemma.Modules.Property.Features.ChangeIssueStatus;
using Hemma.Modules.Property.Features.ChangeProjectStatus;
using Hemma.Modules.Property.Features.CompleteOccurrence;
using Hemma.Modules.Property.Features.CreateArea;
using Hemma.Modules.Property.Features.CreateHistoryEntry;
using Hemma.Modules.Property.Features.CreateMaintenancePlan;
using Hemma.Modules.Property.Features.CreateProject;
using Hemma.Modules.Property.Features.CreateTag;
using Hemma.Modules.Property.Features.DeactivatePlan;
using Hemma.Modules.Property.Features.DeleteHistoryEntry;
using Hemma.Modules.Property.Features.DeleteIssue;
using Hemma.Modules.Property.Features.DeletePlan;
using Hemma.Modules.Property.Features.DeleteProject;
using Hemma.Modules.Property.Features.DeleteTask;
using Hemma.Modules.Property.Features.GetAttachmentContent;
using Hemma.Modules.Property.Features.GetHistoryPhoto;
using Hemma.Modules.Property.Features.GetIssue;
using Hemma.Modules.Property.Features.GetMaintenancePlan;
using Hemma.Modules.Property.Features.GetProject;
using Hemma.Modules.Property.Features.GetProjectBudget;
using Hemma.Modules.Property.Features.GetProjectTasks;
using Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;
using Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;
using Hemma.Modules.Property.Features.ListAreas;
using Hemma.Modules.Property.Features.ListHistory;
using Hemma.Modules.Property.Features.ListIssues;
using Hemma.Modules.Property.Features.ListMaintenancePlans;
using Hemma.Modules.Property.Features.ListProjects;
using Hemma.Modules.Property.Features.ListTags;
using Hemma.Modules.Property.Features.ListUpcomingOccurrences;
using Hemma.Modules.Property.Features.PromoteIssueToProject;
using Hemma.Modules.Property.Features.PromoteOccurrenceToProject;
using Hemma.Modules.Property.Features.RemoveAttachment;
using Hemma.Modules.Property.Features.RemoveLink;
using Hemma.Modules.Property.Features.ReorderAreas;
using Hemma.Modules.Property.Features.ReorderTasks;
using Hemma.Modules.Property.Features.ReportIssue;
using Hemma.Modules.Property.Features.SkipOccurrence;
using Hemma.Modules.Property.Features.UnlinkIssue;
using Hemma.Modules.Property.Features.UpdateArea;
using Hemma.Modules.Property.Features.UpdateHistoryEntry;
using Hemma.Modules.Property.Features.UpdateIssue;
using Hemma.Modules.Property.Features.UpdateMaintenancePlan;
using Hemma.Modules.Property.Features.UpdateProject;
using Hemma.Modules.Property.Features.UpdateTag;
using Hemma.Modules.Property.Features.UpdateTask;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Jobs;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Contracts;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.Shared;

public sealed class IssuesOperations(
    PropertyDbContext db,
    PropertyAuditPublisher audit,
    IClock clock,
    ActivityOperations activity,
    PropertyNotificationDispatcher notifications)
{
    private const int maxListItems = 100;

    public async Task<ErrorOr<IssueResponse>> ReportIssueAsync(ReportIssueCommand cmd, CancellationToken ct)
    {
        var severity = ParseSeverity(cmd.Severity);
        if (severity is null)
        {
            return PropertyErrors.IssueSeverityInvalid;
        }

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var issue = PropertyIssue.Report(
            cmd.HouseholdId,
            cmd.Title,
            cmd.Description,
            areaId.Value.Value,
            severity.Value,
            cmd.DueDate,
            cmd.Notes,
            clock);
        if (issue.IsError)
        {
            return issue.Errors;
        }

        db.Issues.Add(issue.Value);
        var activityResult = activity.Append(
            cmd.HouseholdId,
            PropertyActivityVerb.IssueReported,
            PropertyActivityTargetType.PropertyIssue,
            issue.Value.Id.Value,
            $"Issue \"{issue.Value.Title}\" was reported.",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["severity"] = issue.Value.Severity.ToString(),
                ["status"] = issue.Value.Status.ToString()
            });
        if (activityResult.IsError)
        {
            return activityResult.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.issue.reported", "PropertyIssue", issue.Value.Id.Value, null, ct);
        await NotifyHighSeverityIssueAsync(issue.Value, "reported", ct);
        return IssueResponse.FromIssue(issue.Value, Today);
    }

    public async Task<ErrorOr<IssueResponse>> UpdateIssueAsync(UpdateIssueCommand cmd, CancellationToken ct)
    {
        var issue = await LoadIssueAsync(cmd.HouseholdId, cmd.IssueId, ct);
        if (issue is null)
        {
            return PropertyErrors.IssueNotFound;
        }

        var severity = ParseSeverity(cmd.Severity);
        if (severity is null)
        {
            return PropertyErrors.IssueSeverityInvalid;
        }

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var updated = issue.Update(
            cmd.Title,
            cmd.Description,
            areaId.Value.Value,
            severity.Value,
            cmd.DueDate,
            cmd.Notes);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.issue.updated", "PropertyIssue", issue.Id.Value, null, ct);
        await NotifyHighSeverityIssueAsync(issue, "updated", ct);
        return await EnrichIssueAsync(IssueResponse.FromIssue(issue, Today), includeTags: false, ct);
    }

    public async Task<ErrorOr<IssueResponse>> ChangeIssueStatusAsync(ChangeIssueStatusCommand cmd, CancellationToken ct)
    {
        var status = ParseStatus(cmd.Status);
        if (status is null)
        {
            return PropertyErrors.IssueStatusInvalid;
        }

        var issue = await LoadIssueAsync(cmd.HouseholdId, cmd.IssueId, ct);
        if (issue is null)
        {
            return PropertyErrors.IssueNotFound;
        }

        var previousStatus = issue.Status;
        var changed = issue.ChangeStatus(status.Value, clock);
        if (changed.IsError)
        {
            return changed.Errors;
        }

        var activityResult = activity.Append(
            cmd.HouseholdId,
            PropertyActivityVerb.IssueStatusChanged,
            PropertyActivityTargetType.PropertyIssue,
            issue.Id.Value,
            $"Issue \"{issue.Title}\" moved from {previousStatus} to {issue.Status}.",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["from"] = previousStatus.ToString(),
                ["to"] = issue.Status.ToString()
            });
        if (activityResult.IsError)
        {
            return activityResult.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.issue.status_changed", "PropertyIssue", issue.Id.Value, null, ct);
        return await EnrichIssueAsync(IssueResponse.FromIssue(issue, Today), includeTags: false, ct);
    }

    public async Task<ErrorOr<Deleted>> DeleteIssueAsync(DeleteIssueCommand cmd, CancellationToken ct)
    {
        var issue = await LoadIssueAsync(cmd.HouseholdId, cmd.IssueId, ct);
        if (issue is null)
        {
            return PropertyErrors.IssueNotFound;
        }

        db.Issues.Remove(issue);
        await db.TagAssignments
            .Where(assignment => assignment.HouseholdId == cmd.HouseholdId
                && assignment.TargetType == PropertyTagTargetType.Issue
                && assignment.TargetId == cmd.IssueId)
            .ExecuteDeleteAsync(ct);

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.issue.deleted", "PropertyIssue", cmd.IssueId, null, ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<IssueResponse>> LinkIssueToMaintenancePlanAsync(LinkIssueToMaintenancePlanCommand cmd, CancellationToken ct)
    {
        var issue = await LoadIssueAsync(cmd.HouseholdId, cmd.IssueId, ct);
        if (issue is null)
        {
            return PropertyErrors.IssueNotFound;
        }

        var exists = await db.MaintenancePlans.AnyAsync(plan =>
            plan.HouseholdId == cmd.HouseholdId && plan.Id == new MaintenancePlanId(cmd.MaintenancePlanId), ct);
        if (!exists)
        {
            return PropertyErrors.IssueLinkTargetInvalid;
        }

        var linked = issue.LinkMaintenancePlan(cmd.MaintenancePlanId);
        if (linked.IsError)
        {
            return linked.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.issue.linked", "PropertyIssue", issue.Id.Value, null, ct);
        return await EnrichIssueAsync(IssueResponse.FromIssue(issue, Today), includeTags: false, ct);
    }

    public async Task<ErrorOr<IssueResponse>> LinkIssueToMaintenanceOccurrenceAsync(LinkIssueToMaintenanceOccurrenceCommand cmd, CancellationToken ct)
    {
        var issue = await LoadIssueAsync(cmd.HouseholdId, cmd.IssueId, ct);
        if (issue is null)
        {
            return PropertyErrors.IssueNotFound;
        }

        var exists = await db.MaintenanceOccurrences.AnyAsync(occurrence =>
            occurrence.HouseholdId == cmd.HouseholdId && occurrence.Id == new MaintenanceOccurrenceId(cmd.MaintenanceOccurrenceId), ct);
        if (!exists)
        {
            return PropertyErrors.IssueLinkTargetInvalid;
        }

        var linked = issue.LinkMaintenanceOccurrence(cmd.MaintenanceOccurrenceId);
        if (linked.IsError)
        {
            return linked.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.issue.linked", "PropertyIssue", issue.Id.Value, null, ct);
        return await EnrichIssueAsync(IssueResponse.FromIssue(issue, Today), includeTags: false, ct);
    }

    public async Task<ErrorOr<IssueResponse>> UnlinkIssueAsync(UnlinkIssueCommand cmd, CancellationToken ct)
    {
        var issue = await LoadIssueAsync(cmd.HouseholdId, cmd.IssueId, ct);
        if (issue is null)
        {
            return PropertyErrors.IssueNotFound;
        }

        var unlinked = issue.Unlink();
        if (unlinked.IsError)
        {
            return unlinked.Errors;
        }

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.issue.unlinked", "PropertyIssue", issue.Id.Value, null, ct);
        return await EnrichIssueAsync(IssueResponse.FromIssue(issue, Today), includeTags: false, ct);
    }

    public async Task<ErrorOr<PromoteIssueToProjectResponse>> PromoteIssueToProjectAsync(PromoteIssueToProjectCommand cmd, CancellationToken ct)
    {
        var issue = await LoadIssueAsync(cmd.HouseholdId, cmd.IssueId, ct);
        if (issue is null)
        {
            return PropertyErrors.IssueNotFound;
        }

        var status = ParseProjectStatus(cmd.Status);
        if (status is null)
        {
            return PropertyErrors.ProjectStatusInvalid;
        }

        var priority = ParseProjectPriority(cmd.Priority);
        if (priority is null)
        {
            return PropertyErrors.ProjectPriorityInvalid;
        }

        var areaId = await ValidateAreaAsync(cmd.HouseholdId, cmd.AreaId, ct);
        if (areaId.IsError)
        {
            return areaId.Errors;
        }

        var estimate = ToMoney(cmd.BudgetEstimate);
        if (estimate.IsError)
        {
            return estimate.Errors;
        }

        var project = Project.Create(
            cmd.HouseholdId,
            cmd.Name,
            cmd.Description,
            status.Value,
            areaId.Value.Value,
            priority.Value,
            cmd.TargetStartDate,
            cmd.TargetEndDate,
            estimate.Value.Value,
            cmd.Notes);
        if (project.IsError)
        {
            return project.Errors;
        }

        var promoted = issue.PromoteToProject(project.Value.Id.Value);
        if (promoted.IsError)
        {
            return promoted.Errors;
        }

        db.Projects.Add(project.Value);

        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.project.created", "Project", project.Value.Id.Value, null, ct);
        await audit.PublishAsync(cmd.HouseholdId, "property.issue.promoted", "PropertyIssue", issue.Id.Value, null, ct);
        var issueResponse = await EnrichIssueAsync(IssueResponse.FromIssue(issue, Today), includeTags: false, ct);
        var projectAreaName = await PropertyAreaTagEnrichment.AreaNameAsync(db, cmd.HouseholdId, project.Value.AreaId?.Value, ct);
        var projectResponse = ProjectResponse.FromProject(project.Value, Today) with { AreaName = projectAreaName };
        return new PromoteIssueToProjectResponse(issueResponse, projectResponse);
    }

    public async Task<ErrorOr<IssueResponse>> GetIssueAsync(GetIssueQuery query, CancellationToken ct)
    {
        var issue = await LoadIssueAsync(query.HouseholdId, query.IssueId, ct, tracking: false);
        return issue is null
            ? PropertyErrors.IssueNotFound
            : await EnrichIssueAsync(IssueResponse.FromIssue(issue, Today), includeTags: true, ct);
    }

    public async Task<ErrorOr<ListIssuesResponse>> ListIssuesAsync(ListIssuesQuery query, CancellationToken ct)
    {
        var issues = db.Issues.AsNoTracking().Where(issue => issue.HouseholdId == query.HouseholdId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ParseStatus(query.Status);
            if (status is null)
            {
                return PropertyErrors.IssueStatusInvalid;
            }

            issues = issues.Where(issue => issue.Status == status.Value);
        }

        if (query.AreaId is not null)
        {
            issues = issues.Where(issue => issue.AreaId == new PropertyAreaId(query.AreaId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            var severity = ParseSeverity(query.Severity);
            if (severity is null)
            {
                return PropertyErrors.IssueSeverityInvalid;
            }

            issues = issues.Where(issue => issue.Severity == severity.Value);
        }

        if (query.LinkedProjectId is not null)
        {
            issues = issues.Where(issue => issue.LinkedProjectId == query.LinkedProjectId);
        }

        if (query.IsOverdue is not null)
        {
            var today = Today;
            issues = query.IsOverdue.Value
                ? issues.Where(issue => (issue.Status == PropertyIssueStatus.Open || issue.Status == PropertyIssueStatus.InProgress) && issue.DueDate < today)
                : issues.Where(issue => issue.DueDate == null || issue.DueDate >= today || (issue.Status != PropertyIssueStatus.Open && issue.Status != PropertyIssueStatus.InProgress));
        }

        if (query.TagIds is { Count: > 0 })
        {
            var tagIds = query.TagIds.Distinct().Select(id => new PropertyTagId(id)).ToArray();
            var matchingIssueIds = (await db.TagAssignments
                .AsNoTracking()
                .Where(assignment => assignment.HouseholdId == query.HouseholdId
                    && assignment.TargetType == PropertyTagTargetType.Issue
                    && tagIds.Contains(assignment.TagId))
                .GroupBy(assignment => assignment.TargetId)
                .Where(group => group.Select(assignment => assignment.TagId).Distinct().Count() == tagIds.Length)
                .Select(group => group.Key)
                .ToArrayAsync(ct))
                .Select(id => new PropertyIssueId(id))
                .ToArray();

            issues = issues.Where(issue => matchingIssueIds.Contains(issue.Id));
        }

        var totalCount = await issues.CountAsync(ct);
        var rows = await issues
            .OrderByDescending(issue => issue.ReportedAt)
            .ThenBy(issue => issue.Title)
            .Take(maxListItems + 1)
            .ToArrayAsync(ct);

        var pageRows = rows.Take(maxListItems).ToArray();
        var areaNames = await PropertyAreaTagEnrichment.AreaNameMapAsync(db, query.HouseholdId, ct);
        var tagsByIssue = await PropertyAreaTagEnrichment.TagsByTargetAsync(
            db, query.HouseholdId, PropertyTagTargetType.Issue, pageRows.Select(issue => issue.Id.Value).ToArray(), ct);

        var items = pageRows
            .Select(issue =>
            {
                var response = IssueResponse.FromIssue(issue, Today);
                return response with
                {
                    AreaName = response.AreaId is null ? null : areaNames.GetValueOrDefault(response.AreaId.Value),
                    Tags = tagsByIssue.GetValueOrDefault(issue.Id.Value, [])
                };
            })
            .ToArray();

        return new ListIssuesResponse(items, rows.Length > maxListItems, totalCount);
    }

    private async Task<PropertyIssue?> LoadIssueAsync(Guid householdId, Guid issueId, CancellationToken ct, bool tracking = true)
    {
        var query = db.Issues.Where(issue => issue.HouseholdId == householdId && issue.Id == new PropertyIssueId(issueId));
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(ct);
    }

    private DateOnly Today => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    private async Task<IssueResponse> EnrichIssueAsync(IssueResponse response, bool includeTags, CancellationToken ct)
    {
        var areaName = await PropertyAreaTagEnrichment.AreaNameAsync(db, response.HouseholdId, response.AreaId, ct);
        var tags = includeTags
            ? await PropertyAreaTagEnrichment.TagsForTargetAsync(db, response.HouseholdId, PropertyTagTargetType.Issue, response.IssueId, ct)
            : response.Tags;
        return response with { AreaName = areaName, Tags = tags };
    }

    private async Task NotifyHighSeverityIssueAsync(PropertyIssue issue, string kind, CancellationToken ct)
    {
        if (issue.Severity is not (PropertyIssueSeverity.High or PropertyIssueSeverity.Critical)
            || issue.Status is not (PropertyIssueStatus.Open or PropertyIssueStatus.InProgress))
        {
            return;
        }

        var critical = issue.Severity == PropertyIssueSeverity.Critical;
        await notifications.NotifyHouseholdAsync(
            new PropertyNotification(
                "PropertyIssue",
                issue.Id.Value,
                issue.HouseholdId,
                $"severity_{kind}",
                Today,
                $"property.issue.severity_{kind}",
                critical ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                critical ? $"Critical issue {kind}: {issue.Title}" : $"High priority issue {kind}: {issue.Title}",
                $"\"{issue.Title}\" is marked {issue.Severity}.",
                new NotificationLinkDto($"/property/issues/{issue.Id.Value}", "View issue")),
            ct);
    }

    private static PropertyIssueSeverity? ParseSeverity(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            return PropertyIssueSeverity.Medium;
        }

        return Enum.TryParse<PropertyIssueSeverity>(severity, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
    }

    private static PropertyIssueStatus? ParseStatus(string status) =>
        Enum.TryParse<PropertyIssueStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private static ProjectStatus? ParseProjectStatus(string status) =>
        Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;

    private static ProjectPriority? ParseProjectPriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            return ProjectPriority.Medium;
        }

        return Enum.TryParse<ProjectPriority>(priority, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
    }

    private static ErrorOr<OptionalMoney> ToMoney(MoneyDto? money)
    {
        if (money is null)
        {
            return new OptionalMoney(null);
        }

        var created = Money.Create(money.Amount, money.Currency);
        return created.IsError ? created.Errors : new OptionalMoney(created.Value);
    }

    private async Task<ErrorOr<OptionalAreaId>> ValidateAreaAsync(Guid householdId, Guid? areaId, CancellationToken ct)
    {
        if (areaId is null)
        {
            return new OptionalAreaId(null);
        }

        var typedId = new PropertyAreaId(areaId.Value);
        var exists = await db.Areas.AnyAsync(area => area.HouseholdId == householdId && area.Id == typedId, ct);
        return exists ? new OptionalAreaId(typedId) : PropertyErrors.AreaNotFound;
    }

    private sealed record OptionalAreaId(PropertyAreaId? Value);

    private sealed record OptionalMoney(Money? Value);
}
