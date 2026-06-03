using Hemma.Shared.Kernel.Gdpr;

namespace Hemma.Shared.Kernel.Interfaces;

public interface IPersonalDataExporter
{
    Task<PersonalDataExport> ExportAsync(UserRef user, CancellationToken ct);
}
