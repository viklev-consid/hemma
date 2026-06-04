using ErrorOr;
using Hemma.Modules.Households.Errors;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Households.Domain;

public sealed class Household : AggregateRoot<HouseholdId>, IAuditableEntity
{
    private readonly List<HouseholdMembership> memberships = [];

    private Household(
        HouseholdId id,
        string name,
        HouseholdSlug slug) : base(id)
    {
        Name = name;
        Slug = slug;
    }

    private Household() : base(default!) { }

    public string Name { get; private set; } = null!;
    public HouseholdSlug Slug { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public int OwnerMutationVersion { get; private set; }
    public IReadOnlyCollection<HouseholdMembership> Memberships => memberships;

    public static ErrorOr<Household> Create(
        string name,
        HouseholdSlug slug,
        Guid createdByUserId,
        IClock clock)
    {
        var nameResult = NormalizeName(name);
        if (nameResult.IsError)
        {
            return nameResult.Errors;
        }

        var household = new Household(HouseholdId.New(), nameResult.Value, slug);
        household.memberships.Add(HouseholdMembership.Create(
            household.Id,
            createdByUserId,
            HouseholdRole.Owner,
            clock));

        return household;
    }

    public ErrorOr<Success> Update(string name, HouseholdSlug slug)
    {
        if (IsDeleted)
        {
            return HouseholdsErrors.HouseholdDeleted;
        }

        var nameResult = NormalizeName(name);
        if (nameResult.IsError)
        {
            return nameResult.Errors;
        }

        Name = nameResult.Value;
        Slug = slug;
        return Result.Success;
    }

    public ErrorOr<HouseholdMembership> AddMember(Guid userId, HouseholdRole role, IClock clock)
    {
        if (IsDeleted)
        {
            return HouseholdsErrors.HouseholdDeleted;
        }

        if (memberships.Any(m => m.UserId == userId && m.IsActive))
        {
            return HouseholdsErrors.MemberAlreadyExists;
        }

        var membership = HouseholdMembership.Create(Id, userId, role, clock);
        memberships.Add(membership);
        return membership;
    }

    public ErrorOr<Success> ChangeMemberRole(Guid userId, HouseholdRole role)
    {
        if (IsDeleted)
        {
            return HouseholdsErrors.HouseholdDeleted;
        }

        var membership = FindActiveMembership(userId);
        if (membership is null)
        {
            return HouseholdsErrors.MemberNotFound;
        }

        if (membership.Role == HouseholdRole.Owner && role != HouseholdRole.Owner && CountActiveOwners() == 1)
        {
            return HouseholdsErrors.LastOwnerRequired;
        }

        membership.ChangeRole(role);
        OwnerMutationVersion++;
        return Result.Success;
    }

    public ErrorOr<string> ChangeMemberRole(Guid actorUserId, Guid targetUserId, HouseholdRole role)
    {
        var actor = FindActiveMembership(actorUserId);
        if (actor is null)
        {
            return HouseholdsErrors.MemberNotFound;
        }

        var target = FindActiveMembership(targetUserId);
        if (target is null)
        {
            return HouseholdsErrors.MemberNotFound;
        }

        var requiredRank = Math.Max(target.Role.Rank, role.Rank);
        if (actor.Role.Rank < requiredRank)
        {
            return HouseholdsErrors.RoleEscalationForbidden;
        }

        var oldRole = target.Role.Name;
        var change = ChangeMemberRole(targetUserId, role);
        return change.IsError ? change.Errors : oldRole;
    }

    public ErrorOr<Success> EnsureCanInviteRole(Guid actorUserId, HouseholdRole role)
    {
        var actor = FindActiveMembership(actorUserId);
        if (actor is null)
        {
            return HouseholdsErrors.MemberNotFound;
        }

        return actor.Role.Rank >= role.Rank
            ? Result.Success
            : HouseholdsErrors.RoleEscalationForbidden;
    }

    public ErrorOr<Success> RemoveMember(Guid userId, Guid removedByUserId, IClock clock)
    {
        if (IsDeleted)
        {
            return HouseholdsErrors.HouseholdDeleted;
        }

        var membership = FindActiveMembership(userId);
        if (membership is null)
        {
            return HouseholdsErrors.MemberNotFound;
        }

        if (membership.Role == HouseholdRole.Owner && CountActiveOwners() == 1)
        {
            return HouseholdsErrors.LastOwnerRequired;
        }

        membership.Remove(removedByUserId, clock);
        OwnerMutationVersion++;
        return Result.Success;
    }

    public ErrorOr<Success> RemoveMemberAsActor(Guid actorUserId, Guid targetUserId, IClock clock)
    {
        var actor = FindActiveMembership(actorUserId);
        if (actor is null)
        {
            return HouseholdsErrors.MemberNotFound;
        }

        var target = FindActiveMembership(targetUserId);
        if (target is null)
        {
            return HouseholdsErrors.MemberNotFound;
        }

        if (actorUserId != targetUserId && actor.Role.Rank < target.Role.Rank)
        {
            return HouseholdsErrors.RoleEscalationForbidden;
        }

        return RemoveMember(targetUserId, actorUserId, clock);
    }

    public ErrorOr<Success> Delete(Guid deletedByUserId, IClock clock)
    {
        if (IsDeleted)
        {
            return Result.Success;
        }

        IsDeleted = true;
        DeletedAt = clock.UtcNow;
        DeletedByUserId = deletedByUserId;

        foreach (var membership in memberships.Where(m => m.IsActive))
        {
            membership.Remove(deletedByUserId, clock);
        }

        OwnerMutationVersion++;
        return Result.Success;
    }

    public HouseholdMembership? FindActiveMembership(Guid userId) =>
        memberships.FirstOrDefault(m => m.UserId == userId && m.IsActive);

    public void AnonymizeUserReferences(Guid userId)
    {
        if (DeletedByUserId == userId)
        {
            DeletedByUserId = null;
        }
    }

    private int CountActiveOwners() =>
        memberships.Count(m => m.IsActive && m.Role == HouseholdRole.Owner);

    private static ErrorOr<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return HouseholdsErrors.NameEmpty;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 200)
        {
            return HouseholdsErrors.NameTooLong;
        }

        return trimmed;
    }
}
