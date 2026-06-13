namespace Hemma.Modules.Property.Domain;

public readonly record struct PropertyIssueId(Guid Value)
{
    public static PropertyIssueId New() => new(Guid.NewGuid());
}
