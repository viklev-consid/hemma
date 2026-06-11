using Hemma.Shared.Infrastructure.Seeding;

namespace Hemma.Modules.Property.Seeding;

internal sealed class PropertyDevSeeder : IModuleSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
