using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class RecurringBillOccurrenceConfiguration : IEntityTypeConfiguration<RecurringBillOccurrence>
{
    public void Configure(EntityTypeBuilder<RecurringBillOccurrence> builder)
    {
        builder.ToTable("recurring_bill_occurrences");

        builder.HasKey(occurrence => occurrence.Id);

        builder.Property(occurrence => occurrence.RecurringBillId)
            .HasConversion(id => id.Value, value => new RecurringBillId(value))
            .IsRequired();

        builder.Property(occurrence => occurrence.DueOn)
            .IsRequired();

        builder.Property(occurrence => occurrence.State)
            .HasConversion(state => state.Name, value => RecurringBillOccurrenceState.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(occurrence => occurrence.TransactionId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? new TransactionId(value.Value) : null);

        builder.Property(occurrence => occurrence.SettlementVersion)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(occurrence => occurrence.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(occurrence => new { occurrence.RecurringBillId, occurrence.DueOn })
            .IsUnique();
    }
}
