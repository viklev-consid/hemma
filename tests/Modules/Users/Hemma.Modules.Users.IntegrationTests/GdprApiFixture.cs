using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Catalog.Persistence;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Organizations.Persistence;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.TestSupport;
using Hemma.TestSupport.Fakes;

namespace Hemma.Modules.Users.IntegrationTests;

[CollectionDefinition("UsersGdpr")]
public sealed class UsersGdprCollection : ICollectionFixture<GdprApiFixture> { }

public sealed class GdprApiFixture : ApiTestFixture
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, FakeEmailSender>();
    }

    protected override async Task MigrateAsync(IServiceProvider services)
    {
        await services.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<OrganizationsDbContext>().Database.MigrateAsync();
    }

    protected override string[] GetSchemasToReset() => ["users", "catalog", "audit", "notifications", "organizations"];
}
