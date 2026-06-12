using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class HistoryEntryConfiguration : IEntityTypeConfiguration<HistoryEntry>
{
    public void Configure(EntityTypeBuilder<HistoryEntry> builder)
    {
        builder.ToTable("history_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasConversion(id => id.Value, value => new HistoryEntryId(value));

        builder.Property(entry => entry.HouseholdId)
            .IsRequired();

        builder.Property(entry => entry.Date)
            .IsRequired();

        builder.Property(entry => entry.Title)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(entry => entry.AreaId)
            .HasConversion<Guid?>(
                id => id == null ? null : id.Value.Value,
                value => value == null ? null : new PropertyAreaId(value.Value));

        builder.OwnsOne(entry => entry.Cost, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("cost_amount")
                .HasColumnType("numeric(18,2)");

            money.Property(value => value.Currency)
                .HasColumnName("cost_currency")
                .HasMaxLength(3);
        });

        builder.Property(entry => entry.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entry => entry.SourceProjectId);
        builder.Property(entry => entry.SourceMaintenanceOccurrenceId);

        builder.HasMany(entry => entry.Photos)
            .WithOne()
            .HasForeignKey(photo => photo.HistoryEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(entry => entry.Photos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(entry => new { entry.HouseholdId, entry.Date });
        builder.HasIndex(entry => new { entry.HouseholdId, entry.AreaId });
        builder.HasIndex(entry => new { entry.HouseholdId, entry.Type });
    }
}
