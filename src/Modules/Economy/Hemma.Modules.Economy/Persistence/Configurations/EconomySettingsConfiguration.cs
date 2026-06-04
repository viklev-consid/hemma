using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class EconomySettingsConfiguration : IEntityTypeConfiguration<EconomySettings>
{
    public void Configure(EntityTypeBuilder<EconomySettings> builder)
    {
        builder.ToTable("economy_settings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.Id)
            .HasConversion(id => id.Value, value => new EconomySettingsId(value));

        builder.Property(settings => settings.HouseholdId)
            .IsRequired();

        builder.Property(settings => settings.CycleStartDay)
            .IsRequired();

        builder.Property(settings => settings.DefaultCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(settings => settings.CreatedOn)
            .IsRequired();

        builder.HasIndex(settings => settings.HouseholdId)
            .IsUnique();
    }
}
