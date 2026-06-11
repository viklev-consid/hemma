using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(transfer => transfer.Id);

        builder.Property(transfer => transfer.Id)
            .HasConversion(id => id.Value, value => new TransferId(value));

        builder.Property(transfer => transfer.HouseholdId)
            .IsRequired();

        builder.Property(transfer => transfer.OutflowTransactionId)
            .HasConversion(id => id.Value, value => new TransactionId(value))
            .IsRequired();

        builder.Property(transfer => transfer.InflowTransactionId)
            .HasConversion(id => id.Value, value => new TransactionId(value))
            .IsRequired();

        builder.Property(transfer => transfer.Mode)
            .HasConversion(mode => mode.Name, value => TransferMode.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(transfer => transfer.OutflowTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(transfer => transfer.InflowTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transfer => new { transfer.HouseholdId, transfer.OutflowTransactionId })
            .IsUnique();

        builder.HasIndex(transfer => new { transfer.HouseholdId, transfer.InflowTransactionId })
            .IsUnique();
    }
}
