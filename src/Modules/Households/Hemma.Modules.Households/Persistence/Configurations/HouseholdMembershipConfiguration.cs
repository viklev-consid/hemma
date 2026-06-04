using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Persistence.Configurations;

internal sealed class HouseholdMembershipConfiguration : IEntityTypeConfiguration<HouseholdMembership>
{
    public void Configure(EntityTypeBuilder<HouseholdMembership> builder)
    {
        builder.ToTable("household_memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new HouseholdMembershipId(value));

        builder.Property(m => m.HouseholdId)
            .HasConversion(id => id.Value, value => new HouseholdId(value));

        builder.Property(m => m.UserId);

        builder.Property(m => m.Role)
            .HasConversion(role => role.Name, value => HouseholdRole.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.Property(m => m.IsAnonymized)
            .IsRequired();

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.CreatedBy).HasMaxLength(100);
        builder.Property(m => m.UpdatedAt);
        builder.Property(m => m.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(m => new { m.HouseholdId, m.UserId })
            .HasFilter("is_active = true")
            .IsUnique();

        builder.HasIndex(m => m.UserId);
    }
}
