using System.Net.Http.Json;
using System.Text.Json;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Features.AreasTags;
using Hemma.Modules.Property.Features.Issues;
using Hemma.Modules.Property.Features.Projects;
using Hemma.Modules.Property.Gdpr;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Infrastructure.Blobs;
using Hemma.Shared.Kernel.Gdpr;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hemma.Modules.Property.IntegrationTests;

[Collection("PropertyModule")]
[Trait("Category", "Integration")]
public sealed class PropertyGdprTests(PropertyApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExportPersonalData_ReturnsTasksAssignedToUser()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Home", "home");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var project = await CreateProjectAsync(client, household.Value, "Kitchen refresh");
        await AddTaskAsync(client, household.Value, project.ProjectId, "Paint the walls", ownerId, new DateOnly(2026, 7, 15));
        // A task assigned to someone else must NOT appear in this user's export.
        await AddTaskAsync(client, household.Value, project.ProjectId, "Someone else's task", Guid.NewGuid(), null);

        var export = await fixture.QueryDbAsync<PropertyDbContext, PersonalDataExport>(async (db, ct) =>
            await new PropertyPersonalDataExporter(db, NullLogger<PropertyPersonalDataExporter>.Instance)
                .ExportAsync(new UserRef(ownerId), ct));

        Assert.Equal(ownerId, export.UserId);
        Assert.Equal("Property", export.ModuleName);

        var assignedTasks = Assert.IsAssignableFrom<System.Collections.IEnumerable>(export.Data["assignedTasks"]);
        Assert.Single(assignedTasks.Cast<object>());

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(export.Data));
        var task = json.RootElement.GetProperty("assignedTasks")[0];
        Assert.Equal("Paint the walls", task.GetProperty("taskTitle").GetString());
        Assert.Equal("Kitchen refresh", task.GetProperty("projectName").GetString());
        Assert.Equal(household.Value, task.GetProperty("householdId").GetGuid());
        Assert.Equal("2026-07-15", task.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task ExportPersonalData_WithNoAssignedTasks_ReturnsEmptyCollection()
    {
        var userId = Guid.NewGuid();

        var export = await fixture.QueryDbAsync<PropertyDbContext, PersonalDataExport>(async (db, ct) =>
            await new PropertyPersonalDataExporter(db, NullLogger<PropertyPersonalDataExporter>.Instance)
                .ExportAsync(new UserRef(userId), ct));

        Assert.Equal(userId, export.UserId);
        var assignedTasks = Assert.IsAssignableFrom<System.Collections.IEnumerable>(export.Data["assignedTasks"]);
        Assert.Empty(assignedTasks.Cast<object>());
    }

    [Fact]
    public async Task EraseHousehold_RemovesHouseholdScopedPropertyData()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Home", "home");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var project = await CreateProjectAsync(client, household.Value, "Kitchen refresh");
        var area = await CreateAreaAsync(client, household.Value);
        var tag = await CreateTagAsync(client, household.Value);
        var issue = await CreateIssueAsync(client, household.Value, area.AreaId);
        await AssignTagAsync(client, household.Value, issue.IssueId, tag.TagId);

        await fixture.ExecuteDbAsync<PropertyDbContext>(async (db, ct) =>
        {
            var eraser = new PropertyPersonalDataEraser(
                db,
                fixture.Services.GetRequiredService<IBlobStore>(),
                NullLogger<PropertyPersonalDataEraser>.Instance);

            var affected = await eraser.EraseHouseholdAsync(household.Value, ct);

            Assert.True(affected >= 4);
        });

        await fixture.QueryDbAsync<PropertyDbContext, object>(async (db, ct) =>
        {
            Assert.False(await db.Projects.AnyAsync(projectEntity => projectEntity.Id == new ProjectId(project.ProjectId), ct));
            Assert.False(await db.Issues.AnyAsync(issueEntity => issueEntity.Id == new PropertyIssueId(issue.IssueId), ct));
            Assert.False(await db.Areas.AnyAsync(areaEntity => areaEntity.HouseholdId == household.Value, ct));
            Assert.False(await db.Tags.AnyAsync(tagEntity => tagEntity.HouseholdId == household.Value, ct));
            Assert.False(await db.TagAssignments.AnyAsync(assignment => assignment.HouseholdId == household.Value, ct));

            return new object();
        });
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, Guid householdId, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/projects",
            new ProjectRequest(householdId, name, null, "Planning", null, null, null, null, null, null));
        response.EnsureSuccessStatusCode();
        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);
        return project;
    }

    private static async Task AddTaskAsync(
        HttpClient client,
        Guid householdId,
        Guid projectId,
        string title,
        Guid? assigneeId,
        DateOnly? dueDate)
    {
        var response = await client.PostAsJsonAsync(
            $"/v1/property/projects/{projectId}/tasks",
            new ProjectTaskRequest(householdId, title, "Todo", null, assigneeId, dueDate));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<PropertyAreaResponse> CreateAreaAsync(HttpClient client, Guid householdId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/areas",
            new PropertyAreaRequest(householdId, "Kitchen", null));
        response.EnsureSuccessStatusCode();
        var area = await response.Content.ReadFromJsonAsync<PropertyAreaResponse>();
        Assert.NotNull(area);
        return area;
    }

    private static async Task<PropertyTagResponse> CreateTagAsync(HttpClient client, Guid householdId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/tags",
            new PropertyTagRequest(householdId, "Safety", "#ef4444"));
        response.EnsureSuccessStatusCode();
        var tag = await response.Content.ReadFromJsonAsync<PropertyTagResponse>();
        Assert.NotNull(tag);
        return tag;
    }

    private static async Task<IssueResponse> CreateIssueAsync(HttpClient client, Guid householdId, Guid areaId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/issues",
            new IssueRequest(householdId, "Ceiling leak", "Water stain near the fixture.", areaId, "High", null, null));
        response.EnsureSuccessStatusCode();
        var issue = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(issue);
        return issue;
    }

    private static async Task AssignTagAsync(HttpClient client, Guid householdId, Guid issueId, Guid tagId)
    {
        var response = await client.PutAsJsonAsync(
            "/v1/property/tags/assignments",
            new AssignTagsRequest(householdId, "Issue", issueId, [tagId]));
        response.EnsureSuccessStatusCode();
    }

    private async Task<HouseholdId> CreateHouseholdAsync(Guid ownerId, string name, string slug)
    {
        await fixture.ExecuteDbAsync<HouseholdsDbContext>(async (db, ct) =>
        {
            var clock = fixture.Services.GetRequiredService<IClock>();
            var household = Household.Create(name, HouseholdSlug.Create(slug).Value, ownerId, clock).Value;
            db.Households.Add(household);
            await db.SaveChangesAsync(ct);
        });

        return await fixture.QueryDbAsync<HouseholdsDbContext, HouseholdId>((db, ct) =>
            db.Households
                .Where(household => household.Slug == HouseholdSlug.Create(slug).Value)
                .Select(household => household.Id)
                .SingleAsync(ct));
    }
}
