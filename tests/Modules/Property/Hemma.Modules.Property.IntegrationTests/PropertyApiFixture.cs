using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Property.Persistence;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Hemma.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.Modules.Property.IntegrationTests;

[CollectionDefinition("PropertyModule")]
public sealed class PropertyModuleFixtureDefinition : ICollectionFixture<PropertyApiFixture> { }

public sealed class PropertyApiFixture : ApiTestFixture
{
    public TestClock Clock { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IClock>(Clock);
    }

    protected override async Task MigrateAsync(IServiceProvider services)
    {
        await services.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<HouseholdsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<EconomyDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<PropertyDbContext>().Database.MigrateAsync();
    }

    protected override string[] GetSchemasToReset() => ["users", "households", "audit", "notifications", "economy", "property"];
}
