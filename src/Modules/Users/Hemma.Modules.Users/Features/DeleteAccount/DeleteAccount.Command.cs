using Hemma.Modules.Users.Domain;

namespace Hemma.Modules.Users.Features.DeleteAccount;

public sealed record DeleteAccountCommand(UserId UserId);
