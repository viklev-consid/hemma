namespace Hemma.Modules.Property.Domain;

public readonly record struct PropertyTagAssignmentId(Guid Value)
{
    public static PropertyTagAssignmentId New() => new(Guid.NewGuid());
}
