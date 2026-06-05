using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasConversion(id => id.Value, value => new TransactionId(value));

        builder.Property(transaction => transaction.HouseholdId)
            .IsRequired();

        builder.Property(transaction => transaction.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .IsRequired();

        builder.Property(transaction => transaction.CategoryId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? new CategoryId(value.Value) : null);

        builder.OwnsOne(transaction => transaction.Amount, money =>
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

        builder.Property(transaction => transaction.OccurredOn)
            .IsRequired();

        builder.Property(transaction => transaction.Note)
            .HasMaxLength(500);

        builder.Property(transaction => transaction.Kind)
            .HasConversion(kind => kind.Name, value => TransactionKind.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(transaction => transaction.ReceiptBlobContainer)
            .HasMaxLength(100);

        builder.Property(transaction => transaction.ReceiptBlobKey)
            .HasMaxLength(200);

        builder.Property(transaction => transaction.TransferId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? new TransferId(value.Value) : null);

        builder.Property(transaction => transaction.IsTransferOutflow)
            .IsRequired();

        builder.Property(transaction => transaction.IsPending)
            .IsRequired();

        builder.Property(transaction => transaction.ImportFingerprint)
            .HasMaxLength(128);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transaction => new { transaction.HouseholdId, transaction.OccurredOn });
        builder.HasIndex(transaction => transaction.AccountId);
        builder.HasIndex(transaction => transaction.CategoryId);
        builder.HasIndex(transaction => transaction.PayerId);
        builder.HasIndex(transaction => transaction.TransferId);
        builder.HasIndex(transaction => new { transaction.AccountId, transaction.ImportFingerprint })
            .IsUnique()
            .HasFilter("\"import_fingerprint\" IS NOT NULL");
    }
}
