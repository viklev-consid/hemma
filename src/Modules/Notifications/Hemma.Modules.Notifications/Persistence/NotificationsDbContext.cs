using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Notifications.Domain;
using Hemma.Shared.Infrastructure.Persistence;

namespace Hemma.Modules.Notifications.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : ModuleDbContext(options)
{
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
