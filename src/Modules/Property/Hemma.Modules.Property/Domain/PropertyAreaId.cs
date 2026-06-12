namespace Hemma.Modules.Property.Domain;

public readonly record struct PropertyAreaId(Guid Value)
{
    public static PropertyAreaId New() => new(Guid.NewGuid());
}
