using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(budget => budget.Id);

        builder.Property(budget => budget.Id)
            .HasConversion(id => id.Value, value => new BudgetId(value));

        builder.Property(budget => budget.HouseholdId)
            .IsRequired();

        builder.Property(budget => budget.PeriodStartsOn)
            .IsRequired();

        builder.Property(budget => budget.PeriodEndsOn)
            .IsRequired();

        builder.HasMany(budget => budget.Lines)
            .WithOne()
            .HasForeignKey(line => line.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(budget => budget.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(budget => new { budget.HouseholdId, budget.PeriodStartsOn })
            .IsUnique();
    }
}
