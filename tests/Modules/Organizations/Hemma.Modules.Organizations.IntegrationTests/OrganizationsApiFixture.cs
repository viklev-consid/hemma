using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Organizations.Persistence;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.TestSupport;
using Hemma.TestSupport.Fakes;

namespace Hemma.Modules.Organizations.IntegrationTests;

[CollectionDefinition("OrganizationsModule")]
public sealed class OrganizationsModuleCollection : ICollectionFixture<OrganizationsApiFixture> { }

[CollectionDefinition("InviteOnlyOrganizationsModule")]
public sealed class InviteOnlyOrganizationsModuleCollection : ICollectionFixture<InviteOnlyOrganizationsApiFixture> { }

public class OrganizationsApiFixture : ApiTestFixture
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, FakeEmailSender>();
    }

    protected override async Task MigrateAsync(IServiceProvider services)
    {
        await services.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<OrganizationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    }

    protected override string[] GetSchemasToReset() => ["users", "organizations", "audit", "notifications"];
}

public sealed class InviteOnlyOrganizationsApiFixture : OrganizationsApiFixture
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Modules:Users:Registration:Mode", "InviteOnly");
    }
}
