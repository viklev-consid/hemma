using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Id)
            .HasConversion(id => id.Value, value => new ProjectId(value));

        builder.Property(project => project.HouseholdId)
            .IsRequired();

        builder.Property(project => project.Name)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(2000);

        builder.Property(project => project.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(project => project.AreaId)
            .HasConversion<Guid?>(
                id => id == null ? null : id.Value.Value,
                value => value == null ? null : new PropertyAreaId(value.Value));

        builder.Property(project => project.Priority)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(ProjectPriority.Medium)
            .IsRequired();

        builder.Property(project => project.TargetStartDate);
        builder.Property(project => project.TargetEndDate);

        builder.OwnsOne(project => project.BudgetEstimate, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("budget_estimate_amount")
                .HasColumnType("numeric(18,2)");

            money.Property(value => value.Currency)
                .HasColumnName("budget_estimate_currency")
                .HasMaxLength(3);
        });

        builder.Property(project => project.CompletedAt);

        builder.Property(project => project.Notes)
            .HasMaxLength(4000);

        builder.HasMany(project => project.Tasks)
            .WithOne()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(project => project.Tasks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(project => project.Links)
            .WithOne()
            .HasForeignKey(link => link.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(project => project.Links)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(project => project.Attachments)
            .WithOne()
            .HasForeignKey(attachment => attachment.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(project => project.Attachments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(project => project.HouseholdId);
        builder.HasIndex(project => new { project.HouseholdId, project.Status });
        builder.HasIndex(project => new { project.HouseholdId, project.AreaId });
        builder.HasIndex(project => new { project.HouseholdId, project.Priority });
    }
}
