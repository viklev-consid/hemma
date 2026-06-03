namespace Hemma.Modules.Users.Domain;

public readonly record struct UserInvitationId(Guid Value)
{
    public static UserInvitationId New() => new(Guid.NewGuid());
}
