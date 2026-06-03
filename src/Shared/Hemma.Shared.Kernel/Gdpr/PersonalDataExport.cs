namespace Hemma.Shared.Kernel.Gdpr;

public sealed record PersonalDataExport(
    Guid UserId,
    string ModuleName,
    IReadOnlyDictionary<string, object?> Data);
