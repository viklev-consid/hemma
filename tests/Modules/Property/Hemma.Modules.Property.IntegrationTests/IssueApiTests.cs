using System.Net;
using System.Net.Http.Json;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Property.Features.AddLink;
using Hemma.Modules.Property.Features.AddTask;
using Hemma.Modules.Property.Features.AssignTags;
using Hemma.Modules.Property.Features.ChangeIssueStatus;
using Hemma.Modules.Property.Features.ChangeProjectStatus;
using Hemma.Modules.Property.Features.CompleteOccurrence;
using Hemma.Modules.Property.Features.CreateArea;
using Hemma.Modules.Property.Features.CreateHistoryEntry;
using Hemma.Modules.Property.Features.CreateMaintenancePlan;
using Hemma.Modules.Property.Features.CreateProject;
using Hemma.Modules.Property.Features.CreateTag;
using Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;
using Hemma.Modules.Property.Features.PromoteIssueToProject;
using Hemma.Modules.Property.Features.PromoteOccurrenceToProject;
using Hemma.Modules.Property.Features.ReorderAreas;
using Hemma.Modules.Property.Features.ReorderTasks;
using Hemma.Modules.Property.Features.ReportIssue;
using Hemma.Modules.Property.Features.Shared;
using Hemma.Modules.Property.Features.SkipOccurrence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.Modules.Property.IntegrationTests;

[Collection("PropertyModule")]
[Trait("Category", "Integration")]
public sealed class IssueApiTests(PropertyApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Issues_CanCreateUpdateFilterTagAndDelete()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Issues", "issues");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 13, 8, 0, 0, TimeSpan.Zero));

        var area = await CreateAreaAsync(client, household.Id.Value, "Kitchen");
        var tag = await CreateTagAsync(client, household.Id.Value, "Safety");

        var created = await client.PostAsJsonAsync(
            "/v1/property/issues",
            new IssueRequest(
                household.Id.Value,
                "Water under sink",
                "Slow drip",
                area.AreaId,
                "High",
                new DateOnly(2026, 6, 12),
                "Bucket in place"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var issue = await created.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(issue);
        Assert.Equal("Open", issue.Status);
        Assert.Equal("High", issue.Severity);
        Assert.Equal(area.AreaId, issue.AreaId);

        var assigned = await client.PutAsJsonAsync(
            "/v1/property/tags/assignments",
            new AssignTagsRequest(household.Id.Value, "Issue", issue.IssueId, [tag.TagId]));
        assigned.EnsureSuccessStatusCode();

        var filtered = await client.GetFromJsonAsync<ListIssuesResponse>(
            $"/v1/property/issues?householdId={household.Id.Value}&status=Open&areaId={area.AreaId}&severity=High&tagIds={tag.TagId}&isOverdue=true");
        Assert.NotNull(filtered);
        Assert.Single(filtered.Issues);
        Assert.Equal(issue.IssueId, filtered.Issues[0].IssueId);
        Assert.True(filtered.Issues[0].IsOverdue);
        Assert.Equal(new DateOnly(2026, 6, 12), filtered.Issues[0].OverdueSince);
        Assert.Equal(1, filtered.Issues[0].DaysOverdue);

        var updated = await client.PutAsJsonAsync(
            $"/v1/property/issues/{issue.IssueId}",
            new IssueRequest(
                household.Id.Value,
                "Water under sink cabinet",
                "Still dripping",
                area.AreaId,
                "Critical",
                new DateOnly(2026, 6, 20),
                "Needs plumber"));
        updated.EnsureSuccessStatusCode();
        var updatedIssue = await updated.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(updatedIssue);
        Assert.Equal("Critical", updatedIssue.Severity);
        Assert.False(updatedIssue.IsOverdue);
        Assert.Null(updatedIssue.OverdueSince);
        Assert.Equal(0, updatedIssue.DaysOverdue);

        var resolved = await client.PostAsJsonAsync(
            $"/v1/property/issues/{issue.IssueId}/status",
            new ChangeIssueStatusRequest(household.Id.Value, "Resolved"));
        resolved.EnsureSuccessStatusCode();
        var resolvedIssue = await resolved.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(resolvedIssue);
        Assert.Equal("Resolved", resolvedIssue.Status);
        Assert.NotNull(resolvedIssue.ResolvedAt);

        var deleted = await client.DeleteAsync(
            $"/v1/property/issues/{issue.IssueId}?householdId={household.Id.Value}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task LinkingIssueToMaintenanceAndUnlinking_Works()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "IssueMaintenance", "issue-maintenance");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 13, 8, 0, 0, TimeSpan.Zero));

        var issue = await CreateIssueAsync(client, household.Id.Value);
        var plan = await CreatePlanAsync(client, household.Id.Value);

        var linkedPlan = await client.PostAsJsonAsync(
            $"/v1/property/issues/{issue.IssueId}/links/maintenance-plan",
            new LinkIssueRequest(household.Id.Value, plan.Plan.PlanId));
        linkedPlan.EnsureSuccessStatusCode();
        var linkedToPlan = await linkedPlan.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(linkedToPlan);
        Assert.Equal(plan.Plan.PlanId, linkedToPlan.LinkedMaintenancePlanId);

        var linkedOccurrence = await client.PostAsJsonAsync(
            $"/v1/property/issues/{issue.IssueId}/links/maintenance-occurrence",
            new LinkIssueRequest(household.Id.Value, plan.NextOccurrence!.OccurrenceId));
        linkedOccurrence.EnsureSuccessStatusCode();
        var linkedToOccurrence = await linkedOccurrence.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(linkedToOccurrence);
        Assert.Equal(plan.NextOccurrence.OccurrenceId, linkedToOccurrence.LinkedMaintenanceOccurrenceId);
        Assert.Null(linkedToOccurrence.LinkedMaintenancePlanId);

        var unlinked = await client.DeleteAsync(
            $"/v1/property/issues/{issue.IssueId}/links?householdId={household.Id.Value}");
        unlinked.EnsureSuccessStatusCode();
        var unlinkedIssue = await unlinked.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(unlinkedIssue);
        Assert.Null(unlinkedIssue.LinkedMaintenanceOccurrenceId);
    }

    [Fact]
    public async Task PromotingIssueToProject_CreatesProjectAndMovesIssueInProgress()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "IssuePromote", "issue-promote");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var issue = await CreateIssueAsync(client, household.Id.Value);

        var promoted = await client.PostAsJsonAsync(
            $"/v1/property/issues/{issue.IssueId}/promote",
            new PromoteIssueToProjectRequest(
                household.Id.Value,
                "Fix roof leak",
                null,
                "Planning",
                null,
                "High",
                null,
                null,
                null,
                null));
        Assert.Equal(HttpStatusCode.Created, promoted.StatusCode);

        var result = await promoted.Content.ReadFromJsonAsync<PromoteIssueToProjectResponse>();
        Assert.NotNull(result);
        Assert.Equal("InProgress", result.Issue.Status);
        Assert.Equal(result.Project.ProjectId, result.Issue.LinkedProjectId);
        Assert.Equal("Fix roof leak", result.Project.Name);
        Assert.Equal("High", result.Project.Priority);
    }

    [Fact]
    public async Task CompletingLinkedProject_ClosesLinkedIssues()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "IssueClose", "issue-close");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 13, 8, 0, 0, TimeSpan.Zero));
        var issue = await CreateIssueAsync(client, household.Id.Value);

        var promoted = await client.PostAsJsonAsync(
            $"/v1/property/issues/{issue.IssueId}/promote",
            new PromoteIssueToProjectRequest(household.Id.Value, "Fix gutter", null, "Active", null, null, null, null, null, null));
        promoted.EnsureSuccessStatusCode();
        var promotion = await promoted.Content.ReadFromJsonAsync<PromoteIssueToProjectResponse>();
        Assert.NotNull(promotion);

        fixture.Clock.Set(new DateTimeOffset(2026, 6, 14, 8, 0, 0, TimeSpan.Zero));
        var completed = await client.PostAsJsonAsync(
            $"/v1/property/projects/{promotion.Project.ProjectId}/status",
            new ChangeProjectStatusRequest(household.Id.Value, "Done"));
        completed.EnsureSuccessStatusCode();

        var closed = await client.GetFromJsonAsync<IssueResponse>(
            $"/v1/property/issues/{issue.IssueId}?householdId={household.Id.Value}");
        Assert.NotNull(closed);
        Assert.Equal("Closed", closed.Status);
        Assert.NotNull(closed.ClosedAt);
    }

    [Fact]
    public async Task CrossHouseholdIssueRead_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "PrivateIssue", "private-issue");
        using var client = fixture.CreateAuthenticatedClient(otherUserId, "other@example.com", "Other");

        var response = await client.GetAsync($"/v1/property/issues?householdId={household.Id.Value}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<IssueResponse> CreateIssueAsync(HttpClient client, Guid householdId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/issues",
            new IssueRequest(householdId, "Loose railing", null, null, null, null, null));
        response.EnsureSuccessStatusCode();
        var issue = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(issue);
        return issue;
    }

    private static async Task<GetMaintenancePlanResponse> CreatePlanAsync(HttpClient client, Guid householdId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/maintenance/plans",
            new MaintenancePlanRequest(householdId, "Boiler", null, null, "Month", 1, new DateOnly(2026, 6, 1), 14));
        response.EnsureSuccessStatusCode();
        var plan = await response.Content.ReadFromJsonAsync<GetMaintenancePlanResponse>();
        Assert.NotNull(plan);
        return plan;
    }

    private static async Task<PropertyAreaResponse> CreateAreaAsync(HttpClient client, Guid householdId, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/areas",
            new PropertyAreaRequest(householdId, name, null));
        response.EnsureSuccessStatusCode();
        var area = await response.Content.ReadFromJsonAsync<PropertyAreaResponse>();
        Assert.NotNull(area);
        return area;
    }

    private static async Task<PropertyTagResponse> CreateTagAsync(HttpClient client, Guid householdId, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/tags",
            new PropertyTagRequest(householdId, name, null));
        response.EnsureSuccessStatusCode();
        var tag = await response.Content.ReadFromJsonAsync<PropertyTagResponse>();
        Assert.NotNull(tag);
        return tag;
    }

    private async Task<(HouseholdId Id, string Slug)> CreateHouseholdAsync(Guid ownerId, string name, string slug)
    {
        await fixture.ExecuteDbAsync<HouseholdsDbContext>(async (db, ct) =>
        {
            var clock = fixture.Services.GetRequiredService<IClock>();
            var household = Household.Create(name, HouseholdSlug.Create(slug).Value, ownerId, clock).Value;
            db.Households.Add(household);
            await db.SaveChangesAsync(ct);
        });

        var householdId = await fixture.QueryDbAsync<HouseholdsDbContext, HouseholdId>((db, ct) =>
            db.Households
                .Where(household => household.Slug == HouseholdSlug.Create(slug).Value)
                .Select(household => household.Id)
                .SingleAsync(ct));

        return (householdId, slug);
    }
}
