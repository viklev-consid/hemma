using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.TestSupport;
using Hemma.TestSupport.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.Modules.Households.IntegrationTests;

[CollectionDefinition("HouseholdsModule")]
public sealed class HouseholdsModuleCollection : ICollectionFixture<HouseholdsApiFixture> { }

[CollectionDefinition("InviteOnlyHouseholdsModule")]
public sealed class InviteOnlyHouseholdsModuleCollection : ICollectionFixture<InviteOnlyHouseholdsApiFixture> { }

public class HouseholdsApiFixture : ApiTestFixture
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, FakeEmailSender>();
    }

    protected override async Task MigrateAsync(IServiceProvider services)
    {
        await services.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<HouseholdsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<EconomyDbContext>().Database.MigrateAsync();
    }

    protected override string[] GetSchemasToReset() => ["users", "households", "audit", "notifications", "economy"];
}

public sealed class InviteOnlyHouseholdsApiFixture : HouseholdsApiFixture
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Modules:Users:Registration:Mode", "InviteOnly");
    }
}
