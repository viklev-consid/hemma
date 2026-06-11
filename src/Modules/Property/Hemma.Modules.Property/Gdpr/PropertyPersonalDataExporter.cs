using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Property.Gdpr;

public sealed class PropertyPersonalDataExporter : IPersonalDataExporter
{
    public Task<PersonalDataExport> ExportAsync(UserRef user, CancellationToken ct)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal);
        return Task.FromResult(new PersonalDataExport(user.UserId, "Property", data));
    }
}
