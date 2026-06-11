namespace Hemma.Modules.Property.Contracts.Authorization;

public static class PropertyPermissions
{
    public const string Read = "property.data.read";
    public const string Write = "property.data.write";

    public static IReadOnlyCollection<string> All { get; } =
        [Read, Write];
}
