using Hemma.Shared.Infrastructure.Seeding;

namespace Hemma.Modules.ModuleName.Seeding;

internal sealed class ModuleNameDevSeeder : IModuleSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
