using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class PropertyAreaConfiguration : IEntityTypeConfiguration<PropertyArea>
{
    public void Configure(EntityTypeBuilder<PropertyArea> builder)
    {
        builder.ToTable("areas");

        builder.HasKey(area => area.Id);

        builder.Property(area => area.Id)
            .HasConversion(id => id.Value, value => new PropertyAreaId(value));

        builder.Property(area => area.HouseholdId)
            .IsRequired();

        builder.Property(area => area.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(area => area.Description)
            .HasMaxLength(1000);

        builder.Property(area => area.SortOrder)
            .IsRequired();

        builder.Property(area => area.IsArchived)
            .IsRequired();

        builder.HasIndex(area => area.HouseholdId);
        builder.HasIndex(area => new { area.HouseholdId, area.Name });
    }
}
