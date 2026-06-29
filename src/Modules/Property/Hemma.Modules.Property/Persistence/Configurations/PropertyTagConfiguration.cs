using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class PropertyTagConfiguration : IEntityTypeConfiguration<PropertyTag>
{
    public void Configure(EntityTypeBuilder<PropertyTag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(tag => tag.Id);

        builder.Property(tag => tag.Id)
            .HasConversion(id => id.Value, value => new PropertyTagId(value));

        builder.Property(tag => tag.HouseholdId)
            .IsRequired();

        builder.Property(tag => tag.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(tag => tag.Color)
            .HasMaxLength(40);

        builder.Property(tag => tag.IsArchived)
            .IsRequired();

        builder.HasIndex(tag => tag.HouseholdId);
        builder.HasIndex(tag => new { tag.HouseholdId, tag.Name });
    }
}
