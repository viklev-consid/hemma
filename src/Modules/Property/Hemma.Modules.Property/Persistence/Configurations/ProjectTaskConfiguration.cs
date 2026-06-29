using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("project_tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .HasConversion(id => id.Value, value => new ProjectTaskId(value));

        builder.Property(task => task.ProjectId)
            .HasConversion(id => id.Value, value => new ProjectId(value))
            .IsRequired();

        builder.Property(task => task.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(task => task.Estimate, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("estimate_amount")
                .HasColumnType("numeric(18,2)");

            money.Property(value => value.Currency)
                .HasColumnName("estimate_currency")
                .HasMaxLength(3);
        });

        builder.Property(task => task.AssigneeId);
        builder.Property(task => task.DueDate);
        builder.Property(task => task.SortOrder).IsRequired();

        builder.HasIndex(task => new { task.ProjectId, task.SortOrder });
        builder.HasIndex(task => task.AssigneeId);
    }
}
