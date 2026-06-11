namespace Hemma.Modules.Households.Contracts.Authorization;

public static class HouseholdsPermissions
{
    public const string HouseholdsRead = "households.households.read";
    public const string HouseholdsWrite = "households.households.write";
    public const string HouseholdsDelete = "households.households.delete";
    public const string MembersRead = "households.members.read";
    public const string MembersManage = "households.members.manage";
    public const string InvitationsManage = "households.invitations.manage";
    public const string AuditRead = "households.audit.read";
    public const string PlatformOverride = "households.platform.override";

    public static IReadOnlyCollection<string> All { get; } =
        [
            HouseholdsRead,
            HouseholdsWrite,
            HouseholdsDelete,
            MembersRead,
            MembersManage,
            InvitationsManage,
            AuditRead,
            PlatformOverride
        ];
}
