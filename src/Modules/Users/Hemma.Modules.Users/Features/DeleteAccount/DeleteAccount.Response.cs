using Hemma.Modules.Households.Contracts.Commands;

namespace Hemma.Modules.Users.Features.DeleteAccount;

public sealed record DeleteAccountResponse(IReadOnlyCollection<UserErasureBlockingHousehold> BlockingHouseholds)
{
    public bool CanDelete => BlockingHouseholds.Count == 0;
}
