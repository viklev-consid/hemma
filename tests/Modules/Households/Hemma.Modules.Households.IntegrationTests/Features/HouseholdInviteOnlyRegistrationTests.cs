using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Users.Features.Register;

namespace Hemma.Modules.Households.IntegrationTests.Features;

[Collection("InviteOnlyHouseholdsModule")]
[Trait("Category", "Integration")]
public sealed class HouseholdInviteOnlyRegistrationTests(InviteOnlyHouseholdsApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_WithHouseholdInvite_WhenUsersInviteOnly_CreatesUserAndMembership()
    {
        var ownerId = Guid.NewGuid();
        var (householdId, rawToken) = await CreateHouseholdInvitationAsync(ownerId);
        using var client = fixture.CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(
            "/v1/users/register",
            new RegisterRequest("invite-only@example.com", "Password1!", "Invite Only", HouseholdInvitationToken: rawToken));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var members = await fixture.QueryDbAsync<HouseholdsDbContext, int>((db, ct) =>
            db.Memberships.CountAsync(m => m.HouseholdId == householdId && m.IsActive, ct));
        Assert.Equal(2, members);
    }

    private async Task<(HouseholdId HouseholdId, string RawToken)> CreateHouseholdInvitationAsync(Guid ownerId)
    {
        string? rawToken = null;
        HouseholdId? householdId = null;

        await fixture.ExecuteDbAsync<HouseholdsDbContext>(async (db, ct) =>
        {
            var clock = fixture.Services.GetRequiredService<Hemma.Shared.Kernel.Interfaces.IClock>();
            var household = Household.Create(
                "Invite Only Org",
                HouseholdSlug.Create("invite-only-org").Value,
                ownerId,
                clock).Value;
            db.Households.Add(household);
            var invitation = HouseholdInvitation.Create(
                household.Id,
                "invite-only@example.com",
                HouseholdRole.Member,
                TimeSpan.FromDays(7),
                ownerId,
                clock).Value;
            db.Invitations.Add(invitation.Invitation);
            await db.SaveChangesAsync(ct);
            householdId = household.Id;
            rawToken = invitation.RawToken;
        });

        return (householdId!, rawToken!);
    }
}
