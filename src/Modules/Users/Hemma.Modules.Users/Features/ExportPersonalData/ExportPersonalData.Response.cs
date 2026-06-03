using Hemma.Shared.Kernel.Gdpr;

namespace Hemma.Modules.Users.Features.ExportPersonalData;

public sealed record ExportPersonalDataResponse(IReadOnlyList<PersonalDataExport> Exports);
