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

namespace Hemma.Modules.Notifications.IntegrationTests;

[CollectionDefinition("NotificationsCrossModule")]
public sealed class NotificationsCrossModuleCollection : ICollectionFixture<NotificationsCrossModuleFixture> { }

public sealed class NotificationsCrossModuleFixture : ApiTestFixture
{
    public FakeEmailSender EmailSender { get; } = new FakeEmailSender();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IEmailSender>(EmailSender);
    }

    protected override async Task MigrateAsync(IServiceProvider services)
    {
        await services.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<OrganizationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    }

    protected override string[] GetSchemasToReset() => ["users", "catalog", "audit", "organizations", "notifications"];
}
