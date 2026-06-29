using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Features.ChangeHouseholdMemberRole;
using Hemma.Modules.Households.Features.CreateHousehold;
using Hemma.Modules.Households.Features.CreateHouseholdInvitation;
using Hemma.Modules.Households.Features.GetHousehold;
using Hemma.Modules.Households.Features.ListHouseholdMembers;
using Hemma.Modules.Households.Features.ListMyHouseholds;
using Hemma.Modules.Households.Gdpr;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Users.Features.Register;
using Hemma.Shared.Kernel.Gdpr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Tracking;

namespace Hemma.Modules.Households.IntegrationTests.Features;

[Collection("HouseholdsModule")]
[Trait("Category", "Integration")]
public sealed class HouseholdSecurityTests(HouseholdsApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MemberOfAnotherHousehold_CannotReadMembers()
    {
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var orgA = await CreateHouseholdAsync(ownerA, "Org A", "org-a");
        await CreateHouseholdAsync(ownerB, "Org B", "org-b");
        using var client = fixture.CreateAuthenticatedClient(ownerA, "a@example.com", "A");

        var response = await client.GetAsync($"/v1/households/org-b/members");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var ownResponse = await client.GetAsync($"/v1/households/{orgA.Slug}/members");
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
    }

    [Fact]
    public async Task Member_CannotPromoteSelfToOwner()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        await AddMemberAsync(org.Id, memberId, HouseholdRole.Member);
        using var memberClient = fixture.CreateAuthenticatedClient(memberId, "member@example.com", "Member");

        var response = await memberClient.PutAsJsonAsync(
            $"/v1/households/{org.Slug}/members/{memberId}/role",
            new ChangeHouseholdMemberRoleRequest("owner"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_CannotCreateOwnerInvitation()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        await AddMemberAsync(org.Id, memberId, HouseholdRole.Member);
        using var memberClient = fixture.CreateAuthenticatedClient(memberId, "member@example.com", "Member");

        var response = await memberClient.PostAsJsonAsync(
            $"/v1/households/{org.Slug}/invitations",
            new CreateHouseholdInvitationRequest("alt@example.com", "owner"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_CannotDeleteHousehold()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        await AddMemberAsync(org.Id, memberId, HouseholdRole.Member);
        using var memberClient = fixture.CreateAuthenticatedClient(memberId, "member@example.com", "Member");

        var response = await memberClient.DeleteAsync($"/v1/households/{org.Slug}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task HouseholdInvite_CanRegisterUserAndCannotBeReplayed()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var ownerClient = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var inviteResponse = await ownerClient.PostAsJsonAsync(
            $"/v1/households/{org.Slug}/invitations",
            new CreateHouseholdInvitationRequest("new@example.com", "member"));
        inviteResponse.EnsureSuccessStatusCode();
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CreateHouseholdInvitationResponse>();
        Assert.NotNull(invite);

        using var anonymous = fixture.CreateAnonymousClient();
        var register = await anonymous.PostAsJsonAsync(
            "/v1/users/register",
            new RegisterRequest("new@example.com", "Password1!", "New User", HouseholdInvitationToken: invite.RawToken));

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var replay = await anonymous.PostAsJsonAsync(
            "/v1/users/register",
            new RegisterRequest("another@example.com", "Password1!", "Another", HouseholdInvitationToken: invite.RawToken));
        Assert.NotEqual(HttpStatusCode.Created, replay.StatusCode);

        var members = await fixture.QueryDbAsync<HouseholdsDbContext, int>((db, ct) =>
            db.Memberships.CountAsync(m => m.HouseholdId == org.Id && m.IsActive, ct));
        Assert.Equal(2, members);
    }

    [Fact]
    public async Task ListMyHouseholds_ReturnsMembershipPermissions()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var response = await client.GetAsync("/v1/households/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListMyHouseholdsResponse>();
        Assert.NotNull(body);
        var household = Assert.Single(body.Households);
        Assert.Equal(org.Id.Value, household.HouseholdId);
        Assert.Equal("owner", household.Role);
        Assert.Contains("households.members.manage", household.Permissions);
    }

    [Fact]
    public async Task PlatformAdminWhoIsMember_GetsScopedPermissionAccessMode()
    {
        var userId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(userId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(userId, "admin@example.com", "Admin", role: "admin");

        var response = await client.GetAsync($"/v1/households/{org.Slug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetHouseholdResponse>();
        Assert.NotNull(body);
        Assert.Equal("ScopedPermission", body.AccessMode);
    }

    [Fact]
    public async Task UserErasureCheck_BlocksOwners()
    {
        using var anonymous = fixture.CreateAnonymousClient();
        var register = await anonymous.PostAsJsonAsync(
            "/v1/users/register",
            new RegisterRequest("owner@example.com", "Password1!", "Owner"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registered);

        using var client = fixture.CreateAuthenticatedClient(registered.UserId, "owner@example.com", "Owner");
        var create = await client.PostAsJsonAsync(
            "/v1/households",
            new CreateHouseholdRequest("Acme", "acme"));
        create.EnsureSuccessStatusCode();

        var response = await client.DeleteAsync("/v1/users/me");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Households.Owner.UserErasureBlocked", problem.RootElement.GetProperty("errorCode").GetString());
        var blocker = Assert.Single(problem.RootElement.GetProperty("blockingHouseholds").EnumerateArray());
        Assert.Equal("acme", blocker.GetProperty("slug").GetString());
        Assert.True(blocker.GetProperty("isSoleOwner").GetBoolean());
    }

    [Fact]
    public async Task LastOwner_CannotLeaveHousehold()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var response = await client.DeleteAsync($"/v1/households/{org.Slug}/members/{ownerId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Member_CanLeaveHousehold()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        await AddMemberAsync(org.Id, memberId, HouseholdRole.Member);
        using var client = fixture.CreateAuthenticatedClient(memberId, "member@example.com", "Member");

        var response = await client.DeleteAsync($"/v1/households/{org.Slug}/members/{memberId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var isActive = await fixture.QueryDbAsync<HouseholdsDbContext, bool>((db, ct) =>
            db.Memberships.AnyAsync(m => m.HouseholdId == org.Id && m.UserId == memberId && m.IsActive, ct));
        Assert.False(isActive);
    }

    [Fact]
    public async Task Member_CannotRemoveAnotherMember()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        await AddMemberAsync(org.Id, targetId, HouseholdRole.Member);
        await AddMemberAsync(org.Id, memberId, HouseholdRole.Member);
        using var client = fixture.CreateAuthenticatedClient(memberId, "member@example.com", "Member");

        var response = await client.DeleteAsync($"/v1/households/{org.Slug}/members/{targetId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_CannotRemoveOwner()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        await AddMemberAsync(org.Id, memberId, HouseholdRole.Member);
        using var client = fixture.CreateAuthenticatedClient(memberId, "member@example.com", "Member");

        var response = await client.DeleteAsync($"/v1/households/{org.Slug}/members/{ownerId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PlatformOverride_CannotMutateHouseholdWithoutScopedPermission()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(Guid.NewGuid(), "admin@example.com", "Admin", role: "admin");

        var response = await client.DeleteAsync($"/v1/households/{org.Slug}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListMembers_RejectsUnboundedPageSize()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var response = await client.GetAsync($"/v1/households/{org.Slug}/members?pageSize=10000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListInvitations_RejectsUnboundedPageSize()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var response = await client.GetAsync($"/v1/households/{org.Slug}/invitations?pageSize=10000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ErasingLastOwner_RevalidatesOwnershipInvariant()
    {
        var ownerId = Guid.NewGuid();
        await CreateHouseholdAsync(ownerId, "Acme", "acme");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.ExecuteDbAsync<HouseholdsDbContext>(async (db, ct) =>
            {
                var clock = fixture.Services.GetRequiredService<Hemma.Shared.Kernel.Interfaces.IClock>();
                var eraser = new HouseholdsPersonalDataEraser(db, clock);
                await eraser.EraseAsync(new UserRef(ownerId), ErasureStrategy.Anonymize, ct);
            }));
    }

    [Fact]
    public async Task ExportPersonalData_ReturnsMembershipData()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");

        var export = await fixture.QueryDbAsync<HouseholdsDbContext, PersonalDataExport>(async (db, ct) =>
            await new HouseholdsPersonalDataExporter(db).ExportAsync(new UserRef(ownerId), ct));

        var memberships = Assert.IsAssignableFrom<System.Collections.IEnumerable>(export.Data["memberships"]);
        Assert.Single(memberships.Cast<object>());
        Assert.Equal(ownerId, export.UserId);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(export.Data));
        Assert.Equal(org.Id.Value, json.RootElement.GetProperty("memberships")[0].GetProperty("householdId").GetGuid());
    }

    [Fact]
    public async Task DeletingMemberAccount_AnonymizesHouseholdMembership()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var anonymous = fixture.CreateAnonymousClient();
        var register = await anonymous.PostAsJsonAsync(
            "/v1/users/register",
            new RegisterRequest("member@example.com", "Password1!", "Member"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registered);
        await AddMemberAsync(org.Id, registered.UserId, HouseholdRole.Member);
        using var memberClient = fixture.CreateAuthenticatedClient(registered.UserId, "member@example.com", "Member");
        HttpResponseMessage? response = null;

        Func<IMessageContext, Task> act = async _ =>
        {
            response = await memberClient.DeleteAsync("/v1/users/me");
        };

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .ExecuteAndWaitAsync(act);

        Assert.Equal(HttpStatusCode.NoContent, response!.StatusCode);
        var membership = await fixture.QueryDbAsync<HouseholdsDbContext, HouseholdMembership>((db, ct) =>
            db.Memberships.SingleAsync(m => m.HouseholdId == org.Id && m.JoinedAt != default && m.Role == HouseholdRole.Member, ct));
        Assert.False(membership.IsActive);
        Assert.True(membership.IsAnonymized);
        Assert.Null(membership.UserId);
        Assert.Null(membership.RemovedByUserId);
    }

    [Fact]
    public async Task AcceptExpiredInvitation_ReturnsValidationFailure()
    {
        var ownerId = Guid.NewGuid();
        var invitedId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        var rawToken = await CreateExpiredInvitationAsync(org.Id, "expired@example.com", ownerId);
        using var client = fixture.CreateAuthenticatedClient(invitedId, "expired@example.com", "Expired");

        var response = await client.PostAsJsonAsync(
            "/v1/households/invitations/accept",
            new { InvitationToken = rawToken });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatingHousehold_PersistsAuditEntryWithHouseholdScope()
    {
        var ownerId = Guid.NewGuid();
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        HttpResponseMessage? response = null;

        Func<IMessageContext, Task> act = async _ =>
        {
            response = await client.PostAsJsonAsync(
                "/v1/households",
                new CreateHouseholdRequest("Audited", "audited"));
        };

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .ExecuteAndWaitAsync(act);

        Assert.Equal(HttpStatusCode.Created, response!.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateHouseholdResponse>();
        Assert.NotNull(body);

        var audit = await fixture.QueryDbAsync<AuditDbContext, bool>((db, ct) =>
            db.AuditEntries.AnyAsync(
                e => e.HouseholdId == body.HouseholdId && e.EventType == "household.created",
                ct));
        Assert.True(audit);

        var auditResponse = await client.GetAsync($"/v1/households/{body.Slug}/audit");

        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        using var auditBody = JsonDocument.Parse(await auditResponse.Content.ReadAsStringAsync());
        Assert.Equal(body.HouseholdId, auditBody.RootElement.GetProperty("householdId").GetGuid());
        Assert.Equal(1, auditBody.RootElement.GetProperty("total").GetInt32());
        var entry = Assert.Single(auditBody.RootElement.GetProperty("entries").EnumerateArray());
        Assert.Equal("household.created", entry.GetProperty("eventType").GetString());
        var payload = entry.GetProperty("payload").GetString();
        Assert.NotNull(payload);
        Assert.Contains("HouseholdId", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("Audited", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("audited", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreatingHouseholdInvitation_PersistsAuditEntryWithoutRawTokenOrEmail()
    {
        var ownerId = Guid.NewGuid();
        var org = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        HttpResponseMessage? response = null;

        Func<IMessageContext, Task> act = async _ =>
        {
            response = await client.PostAsJsonAsync(
                $"/v1/households/{org.Slug}/invitations",
                new CreateHouseholdInvitationRequest("invitee@example.com", "member"));
        };

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .ExecuteAndWaitAsync(act);

        Assert.Equal(HttpStatusCode.OK, response!.StatusCode);
        var invitation = await response.Content.ReadFromJsonAsync<CreateHouseholdInvitationResponse>();
        Assert.NotNull(invitation);

        var payload = await fixture.QueryDbAsync<AuditDbContext, string>((db, ct) =>
            db.AuditEntries
                .Where(e => e.HouseholdId == org.Id.Value && e.EventType == "household.invitation_created")
                .Select(e => e.Payload)
                .SingleAsync(ct));

        Assert.Contains(invitation.InvitationId.ToString(), payload, StringComparison.Ordinal);
        Assert.DoesNotContain(invitation.RawToken, payload, StringComparison.Ordinal);
        Assert.DoesNotContain("invitee@example.com", payload, StringComparison.Ordinal);
    }

    private async Task<(HouseholdId Id, string Slug)> CreateHouseholdAsync(Guid ownerId, string name, string slug)
    {
        await fixture.ExecuteDbAsync<HouseholdsDbContext>(async (db, ct) =>
        {
            var clock = fixture.Services.GetRequiredService<Hemma.Shared.Kernel.Interfaces.IClock>();
            var household = Household.Create(name, HouseholdSlug.Create(slug).Value, ownerId, clock).Value;
            db.Households.Add(household);
            await db.SaveChangesAsync(ct);
        });

        var householdId = await fixture.QueryDbAsync<HouseholdsDbContext, HouseholdId>((db, ct) =>
            db.Households
                .Where(o => o.Slug == HouseholdSlug.Create(slug).Value)
                .Select(o => o.Id)
                .SingleAsync(ct));

        return (householdId, slug);
    }

    private async Task AddMemberAsync(HouseholdId householdId, Guid userId, HouseholdRole role)
    {
        await fixture.ExecuteDbAsync<HouseholdsDbContext>(async (db, ct) =>
        {
            var clock = fixture.Services.GetRequiredService<Hemma.Shared.Kernel.Interfaces.IClock>();
            var household = await db.Households.Include(o => o.Memberships).SingleAsync(o => o.Id == householdId, ct);
            var add = household.AddMember(userId, role, clock);
            Assert.False(add.IsError);
            await db.SaveChangesAsync(ct);
        });
    }

    private async Task<string> CreateExpiredInvitationAsync(HouseholdId householdId, string email, Guid invitedByUserId)
    {
        string? rawToken = null;
        await fixture.ExecuteDbAsync<HouseholdsDbContext>(async (db, ct) =>
        {
            var clock = fixture.Services.GetRequiredService<Hemma.Shared.Kernel.Interfaces.IClock>();
            var invitation = HouseholdInvitation.Create(
                householdId,
                email,
                HouseholdRole.Member,
                TimeSpan.FromMilliseconds(1),
                invitedByUserId,
                clock).Value;
            rawToken = invitation.RawToken;
            db.Invitations.Add(invitation.Invitation);
            await db.SaveChangesAsync(ct);
            await Task.Delay(20, ct);
        });

        return rawToken!;
    }
}
