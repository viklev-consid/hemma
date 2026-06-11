using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Users.Persistence;
using Hemma.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hemma.Modules.Property.IntegrationTests.Gdpr;

[CollectionDefinition("UnmigratedProperty")]
public sealed class UnmigratedPropertyFixtureDefinition : ICollectionFixture<UnmigratedPropertyFixture> { }

/// <summary>
/// Boots the full host but deliberately does NOT migrate the Property schema, so the
/// Property tables are genuinely absent — the exact condition the GDPR eraser tolerates
/// on hosts where Property migrations have not yet run.
/// </summary>
public sealed class UnmigratedPropertyFixture : ApiTestFixture
{
    protected override async Task MigrateAsync(IServiceProvider services)
    {
        // Every module EXCEPT Property — leaving the property schema's tables undefined.
        await services.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<HouseholdsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<EconomyDbContext>().Database.MigrateAsync();
    }

    protected override string[] GetSchemasToReset() => ["users", "households", "audit", "notifications", "economy"];
}
