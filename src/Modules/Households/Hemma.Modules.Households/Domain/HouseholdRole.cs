using ErrorOr;
using Hemma.Modules.Households.Errors;

namespace Hemma.Modules.Households.Domain;

public sealed record HouseholdRole
{
    public static readonly HouseholdRole Owner = new("owner");
    public static readonly HouseholdRole Member = new("member");

    private static readonly Dictionary<string, HouseholdRole> known =
        new Dictionary<string, HouseholdRole>(StringComparer.OrdinalIgnoreCase)
        {
            [Owner.Name] = Owner,
            [Member.Name] = Member
        };

    private HouseholdRole(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static IReadOnlyCollection<HouseholdRole> All { get; } =
        [Owner, Member];

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
