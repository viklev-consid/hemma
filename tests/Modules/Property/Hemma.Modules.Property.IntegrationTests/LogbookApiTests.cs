using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hemma.Modules.Economy.Domain;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Property.Features.Logbook;
using Hemma.Modules.Property.Features.Projects;
using Hemma.Shared.Contracts;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.Modules.Property.IntegrationTests;

[Collection("PropertyModule")]
[Trait("Category", "Integration")]
public sealed class LogbookApiTests(PropertyApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HistoryEntries_CanCreateListUpdateAndDelete()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "History", "history");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var created = await client.PostAsJsonAsync(
            "/v1/property/history",
            new HistoryEntryRequest(
                household.Id.Value,
                new DateOnly(2026, 5, 1),
                "Painted hallway",
                "Hallway",
                new MoneyDto(1200m, "SEK"),
                "Manual",
                null,
                null,
                []));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var entry = await created.Content.ReadFromJsonAsync<HistoryEntryResponse>();
        Assert.NotNull(entry);
        Assert.Equal("Manual", entry.Type);
        Assert.Empty(entry.Photos);

        var listed = await client.GetFromJsonAsync<ListHistoryResponse>(
            $"/v1/property/history?householdId={household.Id.Value}&year=2026&area=Hallway&type=Manual");
        Assert.NotNull(listed);
        Assert.Single(listed.Entries);

        var updated = await client.PutAsJsonAsync(
            $"/v1/property/history/{entry.HistoryEntryId}",
            new HistoryEntryRequest(
                household.Id.Value,
                new DateOnly(2026, 5, 2),
                "Painted hallway trim",
                "Hallway",
                new MoneyDto(1400m, "SEK"),
                "Manual",
                null,
                null,
                []));
        updated.EnsureSuccessStatusCode();

        var updatedEntry = await updated.Content.ReadFromJsonAsync<HistoryEntryResponse>();
        Assert.NotNull(updatedEntry);
        Assert.Equal(new DateOnly(2026, 5, 2), updatedEntry.Date);
        Assert.Equal(1400m, updatedEntry.Cost!.Amount);

        var deleted = await client.DeleteAsync(
            $"/v1/property/history/{entry.HistoryEntryId}?householdId={household.Id.Value}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var empty = await client.GetFromJsonAsync<ListHistoryResponse>(
            $"/v1/property/history?householdId={household.Id.Value}&year=2026");
        Assert.NotNull(empty);
        Assert.Empty(empty.Entries);
    }

    [Fact]
    public async Task CreatingHistoryEntryFromProjectSuggestion_CopiesPhotoAndSnapshotsCost()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Snapshot", "snapshot");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var project = await CreateProjectAsync(client, household.Id.Value);
        await SeedLinkedTransactionsAsync(household.Id.Value, project.ProjectId, [2500m]);

        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var attachment = await UploadAttachmentAsync(client, household.Id.Value, project.ProjectId, bytes);

        fixture.Clock.Set(new DateTimeOffset(2026, 8, 2, 10, 0, 0, TimeSpan.Zero));
        var completed = await client.PostAsJsonAsync(
            $"/v1/property/projects/{project.ProjectId}/status",
            new ChangeProjectStatusRequest(household.Id.Value, "Done"));
        completed.EnsureSuccessStatusCode();

        var completion = await completed.Content.ReadFromJsonAsync<ChangeProjectStatusResponse>();
        Assert.NotNull(completion);
        Assert.NotNull(completion.SuggestedHistoryEntry);
        Assert.Equal(2500m, completion.SuggestedHistoryEntry.Cost!.Amount);
        Assert.Single(completion.SuggestedHistoryEntry.PhotoRefs);

        var suggested = completion.SuggestedHistoryEntry;
        var historyCreated = await client.PostAsJsonAsync(
            "/v1/property/history",
            new HistoryEntryRequest(
                household.Id.Value,
                suggested.Date,
                suggested.Title,
                suggested.Area,
                suggested.Cost,
                suggested.Type,
                suggested.SourceProjectId,
                suggested.SourceMaintenanceOccurrenceId,
                suggested.PhotoRefs.Select(photo => new HistoryPhotoRefRequest(photo.Container, photo.Key)).ToArray()));
        historyCreated.EnsureSuccessStatusCode();

        var history = await historyCreated.Content.ReadFromJsonAsync<HistoryEntryResponse>();
        Assert.NotNull(history);
        Assert.Equal(2500m, history.Cost!.Amount);
        Assert.Single(history.Photos);
        Assert.NotEqual(suggested.PhotoRefs[0].Key, history.Photos[0].Key);

        var deletedAttachment = await client.DeleteAsync(
            $"/v1/property/projects/{project.ProjectId}/attachments/{attachment.AttachmentId}?householdId={household.Id.Value}");
        Assert.Equal(HttpStatusCode.NoContent, deletedAttachment.StatusCode);

        var downloaded = await client.GetAsync(
            $"/v1/property/history/{history.HistoryEntryId}/photos/{history.Photos[0].Key}/content?householdId={household.Id.Value}");
        downloaded.EnsureSuccessStatusCode();
        Assert.Equal(bytes, await downloaded.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task CreatingHistoryEntryWithAnotherHouseholdsPhotoRef_ReturnsNotFound()
    {
        var sourceOwnerId = Guid.NewGuid();
        var targetOwnerId = Guid.NewGuid();
        var sourceHousehold = await CreateHouseholdAsync(sourceOwnerId, "Source", "source");
        var targetHousehold = await CreateHouseholdAsync(targetOwnerId, "Target", "target");
        using var sourceClient = fixture.CreateAuthenticatedClient(sourceOwnerId, "source@example.com", "Source");
        using var targetClient = fixture.CreateAuthenticatedClient(targetOwnerId, "target@example.com", "Target");

        var project = await CreateProjectAsync(sourceClient, sourceHousehold.Id.Value);
        await UploadAttachmentAsync(sourceClient, sourceHousehold.Id.Value, project.ProjectId, [0x89, 0x50, 0x4E, 0x47]);

        fixture.Clock.Set(new DateTimeOffset(2026, 8, 2, 10, 0, 0, TimeSpan.Zero));
        var completed = await sourceClient.PostAsJsonAsync(
            $"/v1/property/projects/{project.ProjectId}/status",
            new ChangeProjectStatusRequest(sourceHousehold.Id.Value, "Done"));
        completed.EnsureSuccessStatusCode();

        var completion = await completed.Content.ReadFromJsonAsync<ChangeProjectStatusResponse>();
        Assert.NotNull(completion);
        Assert.NotNull(completion.SuggestedHistoryEntry);
        Assert.Single(completion.SuggestedHistoryEntry.PhotoRefs);
        var leakedRef = completion.SuggestedHistoryEntry.PhotoRefs[0];

        var response = await targetClient.PostAsJsonAsync(
            "/v1/property/history",
            new HistoryEntryRequest(
                targetHousehold.Id.Value,
                new DateOnly(2026, 8, 3),
                "Copied photo",
                "Kitchen",
                null,
                "Manual",
                null,
                null,
                [new HistoryPhotoRefRequest(leakedRef.Container, leakedRef.Key)]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CrossHouseholdHistoryRead_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "PrivateHistory", "private-history");
        using var client = fixture.CreateAuthenticatedClient(otherUserId, "other@example.com", "Other");

        var response = await client.GetAsync($"/v1/property/history?householdId={household.Id.Value}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, Guid householdId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/projects",
            new ProjectRequest(householdId, "Project", null, "Planning", "Kitchen", null, null, null, null));
        response.EnsureSuccessStatusCode();
        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(project);
        return project;
    }

    private static async Task<ProjectAttachmentResponse> UploadAttachmentAsync(
        HttpClient client,
        Guid householdId,
        Guid projectId,
        byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "before.png");

        var uploaded = await client.PostAsync(
            $"/v1/property/projects/{projectId}/attachments?householdId={householdId}",
            content);
        uploaded.EnsureSuccessStatusCode();
        var attachment = await uploaded.Content.ReadFromJsonAsync<ProjectAttachmentResponse>();
        Assert.NotNull(attachment);
        return attachment;
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
