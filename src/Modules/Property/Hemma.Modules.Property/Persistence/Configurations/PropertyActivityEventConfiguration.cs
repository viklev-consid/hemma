using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class PropertyActivityEventConfiguration : IEntityTypeConfiguration<PropertyActivityEvent>
{
    public void Configure(EntityTypeBuilder<PropertyActivityEvent> builder)
    {
        builder.ToTable("activity_events");

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id)
            .HasConversion(id => id.Value, value => new PropertyActivityEventId(value));

        builder.Property(activity => activity.HouseholdId)
            .IsRequired();

        builder.Property(activity => activity.OccurredAt)
            .IsRequired();

        builder.Property(activity => activity.ActorId);

        builder.Property(activity => activity.Verb)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(activity => activity.TargetType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(activity => activity.TargetId)
            .IsRequired();

        builder.Property(activity => activity.Summary)
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(activity => activity.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(activity => new { activity.HouseholdId, activity.OccurredAt });
        builder.HasIndex(activity => new { activity.HouseholdId, activity.Verb });
        builder.HasIndex(activity => new { activity.HouseholdId, activity.TargetType, activity.TargetId });
    }
}
