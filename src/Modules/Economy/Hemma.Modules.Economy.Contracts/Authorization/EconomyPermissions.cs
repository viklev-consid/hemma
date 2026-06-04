namespace Hemma.Modules.Economy.Contracts.Authorization;

public static class EconomyPermissions
{
    public const string Read = "economy.data.read";
    public const string Write = "economy.data.write";

    public static IReadOnlyCollection<string> All { get; } =
        [Read, Write];
}
