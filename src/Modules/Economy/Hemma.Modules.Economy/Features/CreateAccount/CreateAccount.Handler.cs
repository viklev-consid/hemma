using ErrorOr;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Errors;
using Hemma.Modules.Economy.Integration;
using Hemma.Modules.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Economy.Features.CreateAccount;

public sealed class CreateAccountHandler(EconomyDbContext db, EconomyAuditPublisher audit)
{
    public async Task<ErrorOr<AccountResponse>> Handle(CreateAccountCommand cmd, CancellationToken ct)
    {
        if (!await db.EconomySettings.AnyAsync(settings => settings.HouseholdId == cmd.HouseholdId, ct))
        {
            return EconomyErrors.SettingsNotFound;
        }

        var type = AccountType.Create(cmd.Type);
        if (type.IsError)
        {
            return type.Errors;
        }

        var money = Money.Create(cmd.OpeningBalanceAmount, cmd.OpeningBalanceCurrency);
        if (money.IsError)
        {
            return money.Errors;
        }

        var account = Account.Create(cmd.HouseholdId, cmd.Name, type.Value, money.Value);
        if (account.IsError)
        {
            return account.Errors;
        }

        db.Accounts.Add(account.Value);
        await db.SaveChangesAsync(ct);
        await audit.PublishAsync(account.Value.HouseholdId, "economy.account.created", "Account", account.Value.Id.Value, null, ct);

        return AccountResponse.From(account.Value);
    }
}
