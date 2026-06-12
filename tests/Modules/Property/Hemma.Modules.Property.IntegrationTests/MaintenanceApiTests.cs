using System.Net;
using System.Net.Http.Json;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Features.Maintenance;
using Hemma.Modules.Property.Features.Projects;
using Hemma.Modules.Property.Jobs;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Hemma.Modules.Property.IntegrationTests;

[Collection("PropertyModule")]
[Trait("Category", "Integration")]
public sealed class MaintenanceApiTests(PropertyApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreatePlan_MaterializesFirstUpcomingOccurrence()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Boiler", "boiler");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));

        var created = await CreatePlanAsync(client, household.Id.Value, "Month", 6, new DateOnly(2026, 1, 15));

        Assert.NotNull(created.NextOccurrence);
        // 2026-01-15 stepped by 6 months to the first date on/after today (2026-06-11) is 2026-07-15.
        Assert.Equal(new DateOnly(2026, 7, 15), created.NextOccurrence.DueDate);
        Assert.Equal("Upcoming", created.NextOccurrence.Status);

        var upcoming = await client.GetFromJsonAsync<ListUpcomingOccurrencesResponse>(
            $"/v1/property/maintenance/occurrences?householdId={household.Id.Value}&horizonDays=120");
        Assert.NotNull(upcoming);
        Assert.Single(upcoming.Occurrences);
        Assert.Equal("Boiler", upcoming.Occurrences[0].PlanTitle);
    }

    [Fact]
    public async Task CompleteOccurrence_ReturnsMaintenanceSuggestedPayloadAndSchedulesNext()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Filter", "filter");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));

        var plan = await CreatePlanAsync(client, household.Id.Value, "Month", 3, new DateOnly(2026, 6, 1));
        var occurrenceId = plan.NextOccurrence!.OccurrenceId;

        var response = await client.PostAsJsonAsync(
            $"/v1/property/maintenance/occurrences/{occurrenceId}/complete",
            new CompleteOccurrenceRequest(household.Id.Value, "Swapped the filter"));
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<CompleteOccurrenceResponse>();
        Assert.NotNull(completion);
        Assert.Equal("Done", completion.Occurrence.Status);
        Assert.NotNull(completion.SuggestedHistoryEntry);
        Assert.Equal("Maintenance", completion.SuggestedHistoryEntry.Type);
        Assert.Null(completion.SuggestedHistoryEntry.Cost);
        Assert.Null(completion.SuggestedHistoryEntry.AreaId);
        Assert.Equal(occurrenceId, completion.SuggestedHistoryEntry.SourceMaintenanceOccurrenceId);

        Assert.NotNull(completion.NextOccurrence);
        Assert.True(completion.NextOccurrence.DueDate > completion.Occurrence.DueDate);
    }

    [Fact]
    public async Task SkipOccurrence_SchedulesNext()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Skip", "skip");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));

        var plan = await CreatePlanAsync(client, household.Id.Value, "Month", 1, new DateOnly(2026, 6, 1));
        var occurrenceId = plan.NextOccurrence!.OccurrenceId;

        var response = await client.PostAsJsonAsync(
            $"/v1/property/maintenance/occurrences/{occurrenceId}/skip",
            new SkipOccurrenceRequest(household.Id.Value, null));
        response.EnsureSuccessStatusCode();

        var skip = await response.Content.ReadFromJsonAsync<SkipOccurrenceResponse>();
        Assert.NotNull(skip);
        Assert.Equal("Skipped", skip.Occurrence.Status);
        Assert.NotNull(skip.NextOccurrence);
    }

    [Fact]
    public async Task PromoteOccurrence_CreatesProjectAndMarksOccurrenceDone()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Promote", "promote");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));

        var plan = await CreatePlanAsync(client, household.Id.Value, "Year", 1, new DateOnly(2026, 6, 1));
        var occurrenceId = plan.NextOccurrence!.OccurrenceId;

        var response = await client.PostAsJsonAsync(
            $"/v1/property/maintenance/occurrences/{occurrenceId}/promote",
            new PromoteOccurrenceRequest(
                household.Id.Value,
                "Repaint facade",
                null,
                "Planning",
                null,
                null,
                null,
                null,
                null,
                null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var promotion = await response.Content.ReadFromJsonAsync<PromoteOccurrenceResponse>();
        Assert.NotNull(promotion);
        Assert.Equal("Done", promotion.Occurrence.Status);
        Assert.Equal(promotion.Project.ProjectId, promotion.Occurrence.SpawnedProjectId);

        var project = await client.GetFromJsonAsync<ProjectResponse>(
            $"/v1/property/projects/{promotion.Project.ProjectId}?householdId={household.Id.Value}");
        Assert.NotNull(project);
        Assert.Equal("Repaint facade", project.Name);
    }

    [Fact]
    public async Task MaterializeJob_NotifiesMembersWithinLeadTime_AndIsIdempotent()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Notify", "notify");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var today = new DateOnly(2026, 6, 11);
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));

        // Anchor today, lead time 14 days -> the first occurrence is due today, inside the window.
        await CreatePlanAsync(client, household.Id.Value, "Month", 1, today, leadTimeDays: 14);

        await RunMaterializeJobAsync(today);
        await RunMaterializeJobAsync(today);

        var notifications = await fixture.QueryDbAsync<NotificationsDbContext, int>((db, ct) =>
            db.UserNotifications.CountAsync(n => n.RecipientUserId == ownerId, ct));

        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task DeactivatedPlan_DoesNotScheduleNextOnCompletion()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Deactivate", "deactivate");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));

        var plan = await CreatePlanAsync(client, household.Id.Value, "Month", 1, new DateOnly(2026, 6, 1));
        var occurrenceId = plan.NextOccurrence!.OccurrenceId;

        var deactivated = await client.PostAsync(
            $"/v1/property/maintenance/plans/{plan.Plan.PlanId}/deactivate?householdId={household.Id.Value}", null);
        deactivated.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            $"/v1/property/maintenance/occurrences/{occurrenceId}/complete",
            new CompleteOccurrenceRequest(household.Id.Value, null));
        response.EnsureSuccessStatusCode();
        var completion = await response.Content.ReadFromJsonAsync<CompleteOccurrenceResponse>();
        Assert.NotNull(completion);
        Assert.Null(completion.NextOccurrence);
    }

    [Fact]
    public async Task DeletePlan_RemovesPlanAndOccurrences()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "DeletePlan", "delete-plan");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));

        var plan = await CreatePlanAsync(client, household.Id.Value, "Month", 1, new DateOnly(2026, 6, 1));

        var deleted = await client.DeleteAsync(
            $"/v1/property/maintenance/plans/{plan.Plan.PlanId}?householdId={household.Id.Value}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var getResponse = await client.GetAsync(
            $"/v1/property/maintenance/plans/{plan.Plan.PlanId}?householdId={household.Id.Value}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var occurrenceCount = await fixture.QueryDbAsync<PropertyDbContext, int>((db, ct) =>
            db.MaintenanceOccurrences.CountAsync(o => o.HouseholdId == household.Id.Value, ct));
        Assert.Equal(0, occurrenceCount);
    }

    [Fact]
    public async Task CrossHouseholdList_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "PrivatePlan", "private-plan");
        using var client = fixture.CreateAuthenticatedClient(otherUserId, "other@example.com", "Other");

        var response = await client.GetAsync($"/v1/property/maintenance/plans?householdId={household.Id.Value}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CrossHouseholdOccurrenceWrites_ReturnNotFound()
    {
        var ownerId = Guid.NewGuid();
        var sourceHousehold = await CreateHouseholdAsync(ownerId, "OccurrenceSource", "occurrence-source");
        var targetHousehold = await CreateHouseholdAsync(ownerId, "OccurrenceTarget", "occurrence-target");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        fixture.Clock.Set(new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero));

        var completePlan = await CreatePlanAsync(client, sourceHousehold.Id.Value, "Month", 1, new DateOnly(2026, 6, 1));
        var complete = await client.PostAsJsonAsync(
            $"/v1/property/maintenance/occurrences/{completePlan.NextOccurrence!.OccurrenceId}/complete",
            new CompleteOccurrenceRequest(targetHousehold.Id.Value, "Wrong household"));
        Assert.Equal(HttpStatusCode.NotFound, complete.StatusCode);

        var skipPlan = await CreatePlanAsync(client, sourceHousehold.Id.Value, "Month", 1, new DateOnly(2026, 7, 1));
        var skip = await client.PostAsJsonAsync(
            $"/v1/property/maintenance/occurrences/{skipPlan.NextOccurrence!.OccurrenceId}/skip",
            new SkipOccurrenceRequest(targetHousehold.Id.Value, "Wrong household"));
        Assert.Equal(HttpStatusCode.NotFound, skip.StatusCode);

        var promotePlan = await CreatePlanAsync(client, sourceHousehold.Id.Value, "Year", 1, new DateOnly(2026, 6, 1));
        var promote = await client.PostAsJsonAsync(
            $"/v1/property/maintenance/occurrences/{promotePlan.NextOccurrence!.OccurrenceId}/promote",
            new PromoteOccurrenceRequest(
                targetHousehold.Id.Value,
                "Wrong household project",
                null,
                "Planning",
                null,
                null,
                null,
                null,
                null,
                null));
        Assert.Equal(HttpStatusCode.NotFound, promote.StatusCode);
    }

    private static async Task<GetMaintenancePlanResponse> CreatePlanAsync(
        HttpClient client,
        Guid householdId,
        string recurrenceUnit,
        int interval,
        DateOnly anchorDate,
        Guid? areaId = null,
        int leadTimeDays = 14)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/property/maintenance/plans",
            new MaintenancePlanRequest(householdId, "Boiler", null, areaId, recurrenceUnit, interval, anchorDate, leadTimeDays));
        response.EnsureSuccessStatusCode();
        var plan = await response.Content.ReadFromJsonAsync<GetMaintenancePlanResponse>();
        Assert.NotNull(plan);
        return plan;
    }

    private async Task RunMaterializeJobAsync(DateOnly today)
    {
        using var scope = fixture.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(new MaterializeMaintenanceOccurrences(today));
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
