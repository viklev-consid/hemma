using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class PropertyIssueConfiguration : IEntityTypeConfiguration<PropertyIssue>
{
    public void Configure(EntityTypeBuilder<PropertyIssue> builder)
    {
        builder.ToTable("issues");

        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.Id)
            .HasConversion(id => id.Value, value => new PropertyIssueId(value));

        builder.Property(issue => issue.HouseholdId)
            .IsRequired();

        builder.Property(issue => issue.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(issue => issue.Description)
            .HasMaxLength(2000);

        builder.Property(issue => issue.AreaId)
            .HasConversion<Guid?>(
                id => id == null ? null : id.Value.Value,
                value => value == null ? null : new PropertyAreaId(value.Value));

        builder.Property(issue => issue.Severity)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(issue => issue.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(issue => issue.ReportedAt)
            .IsRequired();

        builder.Property(issue => issue.DueDate);
        builder.Property(issue => issue.ResolvedAt);
        builder.Property(issue => issue.ClosedAt);
        builder.Property(issue => issue.LinkedProjectId);
        builder.Property(issue => issue.LinkedMaintenancePlanId);
        builder.Property(issue => issue.LinkedMaintenanceOccurrenceId);

        builder.Property(issue => issue.Notes)
            .HasMaxLength(4000);

        builder.HasIndex(issue => issue.HouseholdId);
        builder.HasIndex(issue => new { issue.HouseholdId, issue.Status });
        builder.HasIndex(issue => new { issue.HouseholdId, issue.AreaId });
        builder.HasIndex(issue => new { issue.HouseholdId, issue.Severity });
        builder.HasIndex(issue => new { issue.HouseholdId, issue.LinkedProjectId });
        builder.HasIndex(issue => new { issue.HouseholdId, issue.DueDate });
    }
}
