using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class MaintenanceOccurrenceConfiguration : IEntityTypeConfiguration<MaintenanceOccurrence>
{
    public void Configure(EntityTypeBuilder<MaintenanceOccurrence> builder)
    {
        builder.ToTable("maintenance_occurrences");

        builder.HasKey(occurrence => occurrence.Id);

        builder.Property(occurrence => occurrence.Id)
            .HasConversion(id => id.Value, value => new MaintenanceOccurrenceId(value));

        builder.Property(occurrence => occurrence.PlanId)
            .HasConversion(id => id.Value, value => new MaintenancePlanId(value))
            .IsRequired();

        builder.Property(occurrence => occurrence.HouseholdId)
            .IsRequired();

        builder.Property(occurrence => occurrence.DueDate)
            .IsRequired();

        builder.Property(occurrence => occurrence.OriginalDueDate)
            .IsRequired();

        builder.Property(occurrence => occurrence.SnoozedUntil);

        builder.Property(occurrence => occurrence.SnoozedAt);

        builder.Property(occurrence => occurrence.SnoozeReason)
            .HasMaxLength(2000);

        builder.Property(occurrence => occurrence.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(occurrence => occurrence.CompletedAt);

        builder.Property(occurrence => occurrence.Notes)
            .HasMaxLength(2000);

        builder.Property(occurrence => occurrence.SpawnedProjectId);

        builder.HasIndex(occurrence => new { occurrence.HouseholdId, occurrence.Status, occurrence.DueDate });
        builder.HasIndex(occurrence => new { occurrence.PlanId, occurrence.Status });
        builder.HasIndex(occurrence => new { occurrence.PlanId, occurrence.DueDate }).IsUnique();
    }
}
