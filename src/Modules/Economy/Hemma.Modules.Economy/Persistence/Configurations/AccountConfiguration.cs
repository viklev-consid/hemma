using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasConversion(id => id.Value, value => new AccountId(value));

        builder.Property(account => account.HouseholdId)
            .IsRequired();

        builder.Property(account => account.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(account => account.Type)
            .HasConversion(type => type.Name, value => AccountType.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(account => account.OpeningBalance, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("opening_balance_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            money.Property(value => value.Currency)
                .HasColumnName("opening_balance_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasIndex(account => new { account.HouseholdId, account.Name })
            .IsUnique();
    }
}
