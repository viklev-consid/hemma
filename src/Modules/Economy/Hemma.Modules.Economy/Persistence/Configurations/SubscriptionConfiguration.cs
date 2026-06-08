using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(subscription => subscription.Id);

        builder.Property(subscription => subscription.Id)
            .HasConversion(id => id.Value, value => new SubscriptionId(value));

        builder.Property(subscription => subscription.HouseholdId)
            .IsRequired();

        builder.Property(subscription => subscription.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.OwnsOne(subscription => subscription.Cadence, cadence =>
        {
            cadence.Property(value => value.Frequency)
                .HasColumnName("cadence_frequency")
                .HasMaxLength(32)
                .IsRequired();

            cadence.Property(value => value.Interval)
                .HasColumnName("cadence_interval")
                .IsRequired();

            cadence.Property(value => value.ChargeDay)
                .HasColumnName("cadence_charge_day")
                .IsRequired();
        });

        builder.OwnsOne(subscription => subscription.ExpectedAmount, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("expected_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.Property(value => value.Currency)
                .HasColumnName("expected_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(subscription => subscription.LifecycleState)
            .HasConversion(state => state.Name, value => SubscriptionLifecycleState.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(subscription => subscription.TrialEndsOn);
        builder.Property(subscription => subscription.TrialReminderSentForTrialEndsOn);

        builder.Property(subscription => subscription.AccountId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? new AccountId(value.Value) : null);

        builder.Property(subscription => subscription.StartsOn)
            .IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(subscription => subscription.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(subscription => new { subscription.HouseholdId, subscription.Name });
        builder.HasIndex(subscription => new { subscription.HouseholdId, subscription.TrialEndsOn });
    }
}
