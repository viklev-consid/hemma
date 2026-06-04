using Hemma.Modules.Households.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Households.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class HouseholdTests
{
    private static readonly Guid ownerId = Guid.NewGuid();
    private readonly TestClock clock = new(DateTimeOffset.UtcNow);

    [Fact]
    public void Create_AddsCreatorAsOwner()
    {
        var slug = HouseholdSlug.Create("acme").Value;

        var result = Household.Create("Acme", slug, ownerId, clock);

        Assert.False(result.IsError);
        var household = result.Value;
        var owner = Assert.Single(household.Memberships);
        Assert.Equal(ownerId, owner.UserId);
        Assert.Equal(HouseholdRole.Owner, owner.Role);
        Assert.True(owner.IsActive);
    }

    [Fact]
    public void RemoveMember_WhenLastOwner_ReturnsError()
    {
        var household = CreateHousehold();

        var result = household.RemoveMember(ownerId, ownerId, clock);

        Assert.True(result.IsError);
        Assert.True(household.FindActiveMembership(ownerId)?.IsActive);
    }

    [Fact]
    public void ChangeMemberRole_WhenLastOwnerWouldBeDemoted_ReturnsError()
    {
        var household = CreateHousehold();

        var result = household.ChangeMemberRole(ownerId, HouseholdRole.Admin);

        Assert.True(result.IsError);
        Assert.Equal(HouseholdRole.Owner, household.FindActiveMembership(ownerId)?.Role);
    }

    [Fact]
    public void Delete_WhenLastOwner_SucceedsAndRemovesMemberships()
    {
        var household = CreateHousehold();

        var result = household.Delete(ownerId, clock);

        Assert.False(result.IsError);
        Assert.True(household.IsDeleted);
        Assert.All(household.Memberships, m => Assert.False(m.IsActive));
    }

    [Fact]
    public void AddMember_WhenAlreadyActive_ReturnsError()
    {
        var household = CreateHousehold();
        var userId = Guid.NewGuid();
        household.AddMember(userId, HouseholdRole.Member, clock);

        var result = household.AddMember(userId, HouseholdRole.Admin, clock);

        Assert.True(result.IsError);
    }

    [Fact]
    public void RemoveMemberAsActor_WhenActorRankIsLowerThanTarget_ReturnsError()
    {
        var household = CreateHousehold();
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        household.AddMember(adminId, HouseholdRole.Admin, clock);
        household.AddMember(memberId, HouseholdRole.Member, clock);

        var result = household.RemoveMemberAsActor(memberId, adminId, clock);

        Assert.True(result.IsError);
        Assert.True(household.FindActiveMembership(adminId)?.IsActive);
    }

    [Fact]
    public void RemoveMemberAsActor_WhenActorRankEqualsTarget_RemovesTarget()
    {
        var household = CreateHousehold();
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        household.AddMember(actorId, HouseholdRole.Admin, clock);
        household.AddMember(targetId, HouseholdRole.Admin, clock);

        var result = household.RemoveMemberAsActor(actorId, targetId, clock);

        Assert.False(result.IsError);
        Assert.Null(household.FindActiveMembership(targetId));
    }

    private Household CreateHousehold() =>
        Household.Create("Acme", HouseholdSlug.Create("acme").Value, ownerId, clock).Value;
}

internal sealed class TestClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = now;
}
