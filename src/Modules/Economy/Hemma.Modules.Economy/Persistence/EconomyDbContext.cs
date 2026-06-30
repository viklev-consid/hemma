using Hemma.Modules.Economy.Domain;
using Hemma.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Persistence;

public sealed class EconomyDbContext(DbContextOptions<EconomyDbContext> options) : ModuleDbContext(options)
{
    public DbSet<EconomySettings> EconomySettings => Set<EconomySettings>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<RecurringBill> RecurringBills => Set<RecurringBill>();
    public DbSet<RecurringBillOccurrence> RecurringBillOccurrences => Set<RecurringBillOccurrence>();
    public DbSet<CategorizationRule> CategorizationRules => Set<CategorizationRule>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<EconomyNotificationPreferences> NotificationPreferences => Set<EconomyNotificationPreferences>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("economy");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomyDbContext).Assembly);
    }
}
