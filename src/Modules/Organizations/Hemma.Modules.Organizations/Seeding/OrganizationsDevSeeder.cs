using Hemma.Shared.Infrastructure.Seeding;

namespace Hemma.Modules.Organizations.Seeding;

internal sealed class OrganizationsDevSeeder : IModuleSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
