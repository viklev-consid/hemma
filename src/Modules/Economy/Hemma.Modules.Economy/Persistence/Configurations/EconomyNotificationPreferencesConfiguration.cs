using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class EconomyNotificationPreferencesConfiguration : IEntityTypeConfiguration<EconomyNotificationPreferences>
{
    public void Configure(EntityTypeBuilder<EconomyNotificationPreferences> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(preferences => preferences.Id);

        builder.Property(preferences => preferences.Id)
            .HasConversion(id => id.Value, value => new EconomyNotificationPreferencesId(value));

        builder.Property(preferences => preferences.HouseholdId)
            .IsRequired();

        builder.Property(preferences => preferences.BudgetAlertsEnabled)
            .IsRequired();

        builder.Property(preferences => preferences.BillAlertsEnabled)
            .IsRequired();

        builder.Property(preferences => preferences.TrialAlertsEnabled)
            .IsRequired();

        builder.Property(preferences => preferences.UpdatedAt)
            .IsRequired();

        builder.HasIndex(preferences => preferences.HouseholdId)
            .IsUnique();
    }
}
