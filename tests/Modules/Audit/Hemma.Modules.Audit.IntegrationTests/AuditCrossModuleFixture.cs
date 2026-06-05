using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.TestSupport;
using Hemma.TestSupport.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.Modules.Audit.IntegrationTests;

[CollectionDefinition("AuditCrossModule")]
public sealed class AuditCrossModuleCollection : ICollectionFixture<AuditCrossModuleFixture> { }

public sealed class AuditCrossModuleFixture : ApiTestFixture
{
    public async Task ConfirmEmailAsync(string email)
    {
        await ExecuteDbAsync<UsersDbContext>(async (db, ct) =>
        {
            var clock = Services.GetRequiredService<Hemma.Shared.Kernel.Interfaces.IClock>();
            var user = await db.Users.FirstAsync(u => u.Email == Email.Create(email).Value, ct);
            user.ConfirmEmail(clock);
            await db.SaveChangesAsync(ct);
        });
    }

    // Suppress SMTP dial attempts from the Notifications handler.
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, FakeEmailSender>();
    }

    protected override async Task MigrateAsync(IServiceProvider services)
    {
        await services.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AuditDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<HouseholdsDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<EconomyDbContext>().Database.MigrateAsync();
    }

    protected override string[] GetSchemasToReset() => ["users", "audit", "notifications", "households", "economy"];
}
