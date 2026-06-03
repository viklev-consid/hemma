namespace Hemma.Modules.Users.Features.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid TargetUserId, string NewRole, Guid ChangedBy);
