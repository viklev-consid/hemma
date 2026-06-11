using Hemma.Modules.Households.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Households.Persistence.Configurations;

internal sealed class HouseholdInvitationConfiguration : IEntityTypeConfiguration<HouseholdInvitation>
{
    public void Configure(EntityTypeBuilder<HouseholdInvitation> builder)
    {
        builder.ToTable("household_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasConversion(id => id.Value, value => new HouseholdInvitationId(value));

        builder.Property(i => i.HouseholdId)
            .HasConversion(id => id.Value, value => new HouseholdId(value));

        builder.Property(i => i.Email)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(i => i.Role)
            .HasConversion(role => role.Name, value => HouseholdRole.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(i => i.TokenHash)
            .IsRequired();

        builder.Property(i => i.IsPending)
            .IsRequired();

        builder.HasIndex(i => i.TokenHash)
            .IsUnique();

        builder.HasIndex(i => new { i.HouseholdId, i.Email })
            .HasFilter("is_pending = true")
            .IsUnique();
    }
}
