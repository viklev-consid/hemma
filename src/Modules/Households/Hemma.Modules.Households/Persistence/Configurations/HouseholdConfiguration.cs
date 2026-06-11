using Hemma.Modules.Households.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Households.Persistence.Configurations;

internal sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("households");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new HouseholdId(value));

        builder.Property(o => o.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.Slug)
            .HasConversion(slug => slug.Value, value => HouseholdSlug.Create(value).Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(o => o.Slug)
            .IsUnique();

        builder.Property(o => o.IsDeleted)
            .IsRequired();

        builder.Property(o => o.OwnerMutationVersion)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedAt);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);

        builder.HasMany(o => o.Memberships)
            .WithOne()
            .HasForeignKey(m => m.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Memberships)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
