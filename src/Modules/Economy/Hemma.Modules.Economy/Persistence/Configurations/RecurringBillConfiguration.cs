using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class RecurringBillConfiguration : IEntityTypeConfiguration<RecurringBill>
{
    public void Configure(EntityTypeBuilder<RecurringBill> builder)
    {
        builder.ToTable("recurring_bills");

        builder.HasKey(bill => bill.Id);

        builder.Property(bill => bill.Id)
            .HasConversion(id => id.Value, value => new RecurringBillId(value));

        builder.Property(bill => bill.HouseholdId)
            .IsRequired();

        builder.Property(bill => bill.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(bill => bill.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .IsRequired();

        builder.Property(bill => bill.CategoryId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? new CategoryId(value.Value) : null);

        builder.OwnsOne(bill => bill.Amount, money =>
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

        builder.OwnsOne(bill => bill.Cadence, cadence =>
        {
            cadence.Property(value => value.Frequency)
                .HasColumnName("cadence_frequency")
                .HasMaxLength(32)
                .IsRequired();

            cadence.Property(value => value.Interval)
                .HasColumnName("cadence_interval")
                .IsRequired();

            cadence.Property(value => value.DayOfMonth)
                .HasColumnName("cadence_day_of_month")
                .IsRequired();
        });

        builder.Property(bill => bill.Type)
            .HasConversion(type => type.Name, value => RecurringBillType.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(bill => bill.Direction)
            .HasConversion(direction => direction.Name, value => RecurringBillDirection.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(bill => bill.StartsOn)
            .IsRequired();

        builder.Property(bill => bill.NextDueOn)
            .IsRequired();

        builder.Property(bill => bill.Note)
            .HasMaxLength(500);

        builder.HasMany(bill => bill.Occurrences)
            .WithOne()
            .HasForeignKey(occurrence => occurrence.RecurringBillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(bill => bill.Occurrences)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(bill => bill.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(bill => bill.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(bill => new { bill.HouseholdId, bill.NextDueOn });
    }
}
