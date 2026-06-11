using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .HasConversion(id => id.Value, value => new CategoryId(value));

        builder.Property(category => category.HouseholdId)
            .IsRequired();

        builder.Property(category => category.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.ParentCategoryId)
            .HasConversion(id => id == null ? (Guid?)null : id.Value, value => value == null ? null : new CategoryId(value.Value));

        builder.Property(category => category.Budgetable)
            .IsRequired();

        builder.HasIndex(category => category.HouseholdId);

        builder.HasIndex(category => new { category.HouseholdId, category.ParentCategoryId, category.Name })
            .IsUnique();
    }
}
