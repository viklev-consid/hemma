using Hemma.Shared.Infrastructure.Seeding;

namespace Hemma.Modules.Households.Seeding;

internal sealed class HouseholdsDevSeeder : IModuleSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
