using Hemma.Modules.Organizations.Contracts.Commands;

namespace Hemma.Modules.Users.Features.DeleteAccount;

public sealed record DeleteAccountResponse(IReadOnlyCollection<UserErasureBlockingOrganization> BlockingOrganizations)
{
    public bool CanDelete => BlockingOrganizations.Count == 0;
}
