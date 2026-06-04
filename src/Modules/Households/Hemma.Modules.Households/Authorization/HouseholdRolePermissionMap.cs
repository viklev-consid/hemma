using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Households.Domain;

namespace Hemma.Modules.Households.Authorization;

internal static class HouseholdRolePermissionMap
{
    private static readonly Dictionary<string, IReadOnlyCollection<string>> permissions =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [HouseholdRole.Owner.Name] =
            [
                HouseholdsPermissions.HouseholdsRead,
                HouseholdsPermissions.HouseholdsWrite,
                HouseholdsPermissions.HouseholdsDelete,
                HouseholdsPermissions.MembersRead,
                HouseholdsPermissions.MembersManage,
                HouseholdsPermissions.InvitationsManage,
                HouseholdsPermissions.AuditRead
            ],
            [HouseholdRole.Admin.Name] =
            [
                HouseholdsPermissions.HouseholdsRead,
                HouseholdsPermissions.HouseholdsWrite,
                HouseholdsPermissions.MembersRead,
                HouseholdsPermissions.MembersManage,
                HouseholdsPermissions.InvitationsManage,
                HouseholdsPermissions.AuditRead
            ],
            [HouseholdRole.Member.Name] =
            [
                HouseholdsPermissions.HouseholdsRead,
                HouseholdsPermissions.MembersRead
            ],
            [HouseholdRole.Viewer.Name] =
            [
                HouseholdsPermissions.HouseholdsRead
            ]
        };

    private static readonly Dictionary<string, string> versions =
        permissions.ToDictionary(
            pair => pair.Key,
            pair => ComputeVersion(pair.Value),
            StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> GetPermissions(HouseholdRole role) =>
        permissions.TryGetValue(role.Name, out var rolePermissions) ? rolePermissions : [];

    public static string GetVersion(HouseholdRole role) =>
        versions.TryGetValue(role.Name, out var version) ? version : ComputeVersion([]);

    private static string ComputeVersion(IReadOnlyCollection<string> rolePermissions)
    {
        var joined = string.Join('\n', rolePermissions.Order(StringComparer.Ordinal));
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(joined));
        return Convert.ToBase64String(hash)[..16].Replace('+', '-').Replace('/', '_');
    }
}
