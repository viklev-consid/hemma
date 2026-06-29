using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class PropertyTagAssignmentConfiguration : IEntityTypeConfiguration<PropertyTagAssignment>
{
    public void Configure(EntityTypeBuilder<PropertyTagAssignment> builder)
    {
        builder.ToTable("tag_assignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .HasConversion(id => id.Value, value => new PropertyTagAssignmentId(value));

        builder.Property(assignment => assignment.HouseholdId)
            .IsRequired();

        builder.Property(assignment => assignment.TagId)
            .HasConversion(id => id.Value, value => new PropertyTagId(value))
            .IsRequired();

        builder.Property(assignment => assignment.TargetType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(assignment => assignment.TargetId)
            .IsRequired();

        builder.HasIndex(assignment => assignment.HouseholdId);
        builder.HasIndex(assignment => new { assignment.HouseholdId, assignment.TargetType, assignment.TargetId });
        builder.HasIndex(assignment => new { assignment.HouseholdId, assignment.TagId, assignment.TargetType, assignment.TargetId })
            .IsUnique();
    }
}
