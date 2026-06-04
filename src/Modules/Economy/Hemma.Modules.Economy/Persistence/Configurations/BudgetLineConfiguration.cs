using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    public void Configure(EntityTypeBuilder<BudgetLine> builder)
    {
        builder.ToTable("budget_lines");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id)
            .HasConversion(id => id.Value, value => new BudgetLineId(value));

        builder.Property(line => line.BudgetId)
            .HasConversion(id => id.Value, value => new BudgetId(value))
            .IsRequired();

        builder.Property(line => line.CategoryId)
            .HasConversion(id => id.Value, value => new CategoryId(value))
            .IsRequired();

        builder.OwnsOne(line => line.Amount, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.Property(value => value.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasIndex(line => new { line.BudgetId, line.CategoryId })
            .IsUnique();
    }
}
