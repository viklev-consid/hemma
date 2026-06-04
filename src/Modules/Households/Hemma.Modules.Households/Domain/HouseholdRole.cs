using ErrorOr;
using Hemma.Modules.Households.Errors;

namespace Hemma.Modules.Households.Domain;

public sealed record HouseholdRole
{
    public static readonly HouseholdRole Owner = new("owner", rank: 3);
    public static readonly HouseholdRole Admin = new("admin", rank: 2);
    public static readonly HouseholdRole Member = new("member", rank: 1);
    public static readonly HouseholdRole Viewer = new("viewer", rank: 0);

    private static readonly Dictionary<string, HouseholdRole> known =
        new Dictionary<string, HouseholdRole>(StringComparer.OrdinalIgnoreCase)
        {
            [Owner.Name] = Owner,
            [Admin.Name] = Admin,
            [Member.Name] = Member,
            [Viewer.Name] = Viewer
        };

    private HouseholdRole(string name, int rank)
    {
        Name = name;
        Rank = rank;
    }

    public string Name { get; }
    public int Rank { get; }

    public static IReadOnlyCollection<HouseholdRole> All { get; } =
        [Owner, Admin, Member, Viewer];

    public static ErrorOr<HouseholdRole> Create(string name)
    {
        if (known.TryGetValue(name.Trim(), out var role))
        {
            return role;
        }

        return HouseholdsErrors.RoleInvalid;
    }

    public override string ToString() => Name;
}
