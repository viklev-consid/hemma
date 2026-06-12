using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class MaintenancePlanConfiguration : IEntityTypeConfiguration<MaintenancePlan>
{
    public void Configure(EntityTypeBuilder<MaintenancePlan> builder)
    {
        builder.ToTable("maintenance_plans");

        builder.HasKey(plan => plan.Id);

        builder.Property(plan => plan.Id)
            .HasConversion(id => id.Value, value => new MaintenancePlanId(value));

        builder.Property(plan => plan.HouseholdId)
            .IsRequired();

        builder.Property(plan => plan.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(plan => plan.Description)
            .HasMaxLength(2000);

        builder.Property(plan => plan.AreaId)
            .HasConversion<Guid?>(
                id => id == null ? null : id.Value.Value,
                value => value == null ? null : new PropertyAreaId(value.Value));

        builder.Property(plan => plan.RecurrenceUnit)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(plan => plan.RecurrenceInterval)
            .IsRequired();

        builder.Property(plan => plan.AnchorDate)
            .IsRequired();

        builder.Property(plan => plan.LeadTimeDays)
            .IsRequired();

        builder.Property(plan => plan.IsActive)
            .IsRequired();

        builder.HasIndex(plan => plan.HouseholdId);
        builder.HasIndex(plan => new { plan.HouseholdId, plan.IsActive });
        builder.HasIndex(plan => new { plan.HouseholdId, plan.AreaId });
    }
}
