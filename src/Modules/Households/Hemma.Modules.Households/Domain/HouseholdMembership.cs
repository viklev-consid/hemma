using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Households.Domain;

public sealed class HouseholdMembership : Entity<HouseholdMembershipId>, IAuditableEntity
{
    private HouseholdMembership(
        HouseholdMembershipId id,
        HouseholdId householdId,
        Guid userId,
        HouseholdRole role,
        DateTimeOffset joinedAt) : base(id)
    {
        HouseholdId = householdId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
        IsActive = true;
    }

    private HouseholdMembership() : base(default!) { }

    public HouseholdId HouseholdId { get; private set; } = null!;
    public Guid? UserId { get; private set; }
    public HouseholdRole Role { get; private set; } = HouseholdRole.Member;
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public Guid? RemovedByUserId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAnonymized { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static HouseholdMembership Create(
        HouseholdId householdId,
        Guid userId,
        HouseholdRole role,
        IClock clock) =>
        new(HouseholdMembershipId.New(), householdId, userId, role, clock.UtcNow);

    public void ChangeRole(HouseholdRole role)
    {
        Role = role;
    }

    public void Remove(Guid removedByUserId, IClock clock)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RemovedAt = clock.UtcNow;
        RemovedByUserId = removedByUserId;
    }

    public void Anonymize()
    {
        UserId = null;
        RemovedByUserId = null;
        IsAnonymized = true;
    }
}
