using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Property.Features.Projects;
using Hemma.Shared.Contracts;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.Modules.Property.IntegrationTests;

[Collection("PropertyModule")]
[Trait("Category", "Integration")]
public sealed class PropertyApiTests(PropertyApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Projects_CanCreateListAndCompleteWithSuggestedHistoryPayload()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Home", "home");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var created = await client.PostAsJsonAsync(
            "/v1/property/projects",
            new ProjectRequest(
                household.Id.Value,
                "Kitchen refresh",
                "Paint and counters",
                "Planning",
                "Kitchen",
                new DateOnly(2026, 7, 1),
                new DateOnly(2026, 8, 1),
                new MoneyDto(10000, "SEK"),
                "Use washable paint"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var project = await created.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);
        Assert.Equal("Kitchen refresh", project.Name);

        var listed = await client.GetFromJsonAsync<ListProjectsResponse>(
            $"/v1/property/projects?householdId={household.Id.Value}&status=Planning&area=Kitchen");
        Assert.NotNull(listed);
        Assert.Single(listed.Projects);

        fixture.Clock.Set(new DateTimeOffset(2026, 8, 2, 10, 0, 0, TimeSpan.Zero));
        var completed = await client.PostAsJsonAsync(
            $"/v1/property/projects/{project.ProjectId}/status",
            new ChangeProjectStatusRequest(household.Id.Value, "Done"));

        completed.EnsureSuccessStatusCode();
        var completion = await completed.Content.ReadFromJsonAsync<ChangeProjectStatusResponse>();
        Assert.NotNull(completion);
        Assert.Equal("Done", completion.Project.Status);
        Assert.NotNull(completion.SuggestedHistoryEntry);
        Assert.Equal(new DateOnly(2026, 8, 2), completion.SuggestedHistoryEntry.Date);
        Assert.Equal(project.ProjectId, completion.SuggestedHistoryEntry.SourceProjectId);
    }

    [Fact]
    public async Task Tasks_CanReorderAndRejectMismatchedSet()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Tasks", "tasks");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var project = await CreateProjectAsync(client, household.Id.Value);

        var first = await AddTaskAsync(client, household.Id.Value, project.ProjectId, "First");
        var second = await AddTaskAsync(client, household.Id.Value, project.ProjectId, "Second");

        var reordered = await client.PostAsJsonAsync(
            $"/v1/property/projects/{project.ProjectId}/tasks/reorder",
            new ReorderTasksRequest(household.Id.Value, [second.TaskId, first.TaskId]));
        reordered.EnsureSuccessStatusCode();

        var tasks = await reordered.Content.ReadFromJsonAsync<GetProjectTasksResponse>();
        Assert.NotNull(tasks);
        Assert.Equal([second.TaskId, first.TaskId], tasks.Tasks.Select(task => task.TaskId).ToArray());

        var invalid = await client.PostAsJsonAsync(
            $"/v1/property/projects/{project.ProjectId}/tasks/reorder",
            new ReorderTasksRequest(household.Id.Value, [first.TaskId]));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Attachments_CanUploadDownloadAndDelete()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Files", "files");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var project = await CreateProjectAsync(client, household.Id.Value);
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "before.png");

        var uploaded = await client.PostAsync(
            $"/v1/property/projects/{project.ProjectId}/attachments?householdId={household.Id.Value}",
            content);
        uploaded.EnsureSuccessStatusCode();
        var attachment = await uploaded.Content.ReadFromJsonAsync<ProjectAttachmentResponse>();
        Assert.NotNull(attachment);

        var downloaded = await client.GetAsync(
            $"/v1/property/projects/{project.ProjectId}/attachments/{attachment.AttachmentId}/content?householdId={household.Id.Value}");
        downloaded.EnsureSuccessStatusCode();
        Assert.Equal(bytes, await downloaded.Content.ReadAsByteArrayAsync());

        var deleted = await client.DeleteAsync(
            $"/v1/property/projects/{project.ProjectId}/attachments/{attachment.AttachmentId}?householdId={household.Id.Value}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var missing = await client.GetAsync(
            $"/v1/property/projects/{project.ProjectId}/attachments/{attachment.AttachmentId}/content?householdId={household.Id.Value}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task LinkCreation_RejectsNonHttpUrl()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Links", "links");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var project = await CreateProjectAsync(client, household.Id.Value);

        var response = await client.PostAsJsonAsync(
            $"/v1/property/projects/{project.ProjectId}/links",
            new ProjectLinkRequest(household.Id.Value, "Bad", "javascript:alert(1)"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task StatusValidation_RejectsNumericEnumString()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Status", "status");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var response = await client.PostAsJsonAsync(
            "/v1/property/projects",
            new ProjectRequest(household.Id.Value, "Project", null, "99", null, null, null, null, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AttachmentUpload_RejectsOversizeBeforeHandlerBuffering()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Oversize", "oversize");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var project = await CreateProjectAsync(client, household.Id.Value);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[10 * 1024 * 1024 + 1]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "large.png");

        var response = await client.PostAsync(
            $"/v1/property/projects/{project.ProjectId}/attachments?householdId={household.Id.Value}",
            content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CrossHouseholdRead_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Private", "private");
        using var client = fixture.CreateAuthenticatedClient(otherUserId, "other@example.com", "Other");

        var response = await client.GetAsync($"/v1/property/projects?householdId={household.Id.Value}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectBudget_CombinesStoredEstimateWithLinkedEconomySpend()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Budget", "budget");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var created = await client.PostAsJsonAsync(
            "/v1/property/projects",
            new ProjectRequest(
                household.Id.Value,
                "Bathroom",
                null,
                "Active",
                "Bathroom",
                null,
                null,
                new MoneyDto(10000, "SEK"),
                null));
        created.EnsureSuccessStatusCode();
        var project = await created.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);

        await SeedLinkedTransactionsAsync(household.Id.Value, project.ProjectId, [3000m, 500m]);

        var budget = await client.GetFromJsonAsync<GetProjectBudgetResponse>(
            $"/v1/property/projects/{project.ProjectId}/budget?householdId={household.Id.Value}");

        Assert.NotNull(budget);
        Assert.Equal(10000m, budget.Estimate!.Amount);
        Assert.Equal(3500m, budget.LinkedTotal.Amount);
        Assert.Equal("SEK", budget.LinkedTotal.Currency);
        Assert.Equal(6500m, budget.Remaining!.Amount);
        Assert.Equal(2, budget.TransactionCount);
    }

    [Fact]
    public async Task GetProjectBudget_WithoutEstimateOrSpend_ReturnsZeroTotals()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Empty", "empty-budget");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var project = await CreateProjectAsync(client, household.Id.Value);

        var budget = await client.GetFromJsonAsync<GetProjectBudgetResponse>(
            $"/v1/property/projects/{project.ProjectId}/budget?householdId={household.Id.Value}");

        Assert.NotNull(budget);
        Assert.Null(budget.Estimate);
        Assert.Equal(0m, budget.LinkedTotal.Amount);
        Assert.Null(budget.Remaining);
        Assert.Equal(0, budget.TransactionCount);
    }

    private async Task SeedLinkedTransactionsAsync(Guid householdId, Guid projectId, IReadOnlyList<decimal> amounts)
    {
        await fixture.ExecuteDbAsync<EconomyDbContext>(async (db, ct) =>
        {
            var account = Account.Create(householdId, "Main", AccountType.Spending, Money.Create(0, "SEK").Value).Value;
            db.Accounts.Add(account);

            foreach (var amount in amounts)
            {
                var transaction = Transaction.Record(
                    householdId,
                    account,
                    category: null,
                    Money.Create(amount, "SEK").Value,
                    new DateOnly(2026, 6, 1),
                    note: null,
                    TransactionKind.Expense,
                    payerId: null).Value;
                transaction.AssignToProject(projectId);
                db.Transactions.Add(transaction);
            }

            await db.SaveChangesAsync(ct);
        });
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, Guid householdId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/projects",
            new ProjectRequest(householdId, "Project", null, "Planning", null, null, null, null, null));
        response.EnsureSuccessStatusCode();
        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);
        return project;
    }

    private static async Task<ProjectTaskResponse> AddTaskAsync(HttpClient client, Guid householdId, Guid projectId, string title)
    {
        var response = await client.PostAsJsonAsync(
            $"/v1/property/projects/{projectId}/tasks",
            new ProjectTaskRequest(householdId, title, "Todo", null, null, null));
        response.EnsureSuccessStatusCode();
        var task = await response.Content.ReadFromJsonAsync<ProjectTaskResponse>();
        Assert.NotNull(task);
        return task;
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
