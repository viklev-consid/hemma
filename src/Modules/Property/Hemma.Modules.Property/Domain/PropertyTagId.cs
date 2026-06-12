namespace Hemma.Modules.Property.Domain;

public readonly record struct PropertyTagId(Guid Value)
{
    public static PropertyTagId New() => new(Guid.NewGuid());
}
