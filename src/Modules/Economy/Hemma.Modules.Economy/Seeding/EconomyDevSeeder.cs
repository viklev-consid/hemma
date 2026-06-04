using Hemma.Shared.Infrastructure.Seeding;

namespace Hemma.Modules.Economy.Seeding;

internal sealed class EconomyDevSeeder : IModuleSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
