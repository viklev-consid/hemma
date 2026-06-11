namespace Hemma.Modules.Property.Domain;

public readonly record struct HistoryEntryId(Guid Value)
{
    public static HistoryEntryId New() => new(Guid.NewGuid());
}
