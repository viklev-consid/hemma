using System.Net;
using System.Net.Http.Json;
using Hemma.Modules.Economy.Features.AddCategory;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;
using Hemma.Modules.Economy.Features.CreateAccount;
using Hemma.Modules.Economy.Features.CreateBudget;
using Hemma.Modules.Economy.Features.CreateEconomySettings;
using Hemma.Modules.Economy.Features.ListAccounts;
using Hemma.Modules.Economy.Features.ListCategories;
using Hemma.Modules.Economy.Features.UpsertBudgetLine;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hemma.Modules.Economy.IntegrationTests;

[Collection("EconomyModule")]
[Trait("Category", "Integration")]
public sealed class EconomyApiTests(EconomyApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SettingsCreation_SeedsCategoriesAndRejectsInvalidCycleDay()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var invalid = await client.PostAsJsonAsync(
            "/v1/economy/settings",
            new CreateEconomySettingsRequest(household.Id.Value, 29, "SEK"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);

        var created = await client.PostAsJsonAsync(
            "/v1/economy/settings",
            new CreateEconomySettingsRequest(household.Id.Value, 1, "SEK"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var settings = await created.Content.ReadFromJsonAsync<CreateEconomySettingsResponse>();
        Assert.NotNull(settings);

        var categories = await client.GetFromJsonAsync<ListCategoriesResponse>(
            $"/v1/economy/categories?householdId={household.Id.Value}");

        Assert.NotNull(categories);
        Assert.Contains(categories.Categories, category => string.Equals(category.Name, "Food", StringComparison.Ordinal));
        Assert.Contains(categories.Categories, category => string.Equals(category.Name, "Savings", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Accounts_CanCreateAndList()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);

        var created = await client.PostAsJsonAsync(
            "/v1/economy/accounts",
            new CreateAccountRequest(household.Id.Value, "Checking", "Spending", new MoneyRequest(100, "SEK")));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var accounts = await client.GetFromJsonAsync<ListAccountsResponse>(
            $"/v1/economy/accounts?householdId={household.Id.Value}");

        Assert.NotNull(accounts);
        var account = Assert.Single(accounts.Accounts);
        Assert.Equal("Checking", account.Name);
        Assert.Equal(100, account.OpeningBalance.Amount);
        Assert.Equal("SEK", account.OpeningBalance.Currency);
    }

    [Fact]
    public async Task AddingThirdLevelCategory_ReturnsValidationFailure()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);

        var rootResponse = await client.PostAsJsonAsync(
            "/v1/economy/categories",
            new AddCategoryRequest(household.Id.Value, "Root", null, false));
        rootResponse.EnsureSuccessStatusCode();
        var root = await rootResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(root);

        var childResponse = await client.PostAsJsonAsync(
            "/v1/economy/categories",
            new AddCategoryRequest(household.Id.Value, "Child", root.CategoryId, true));
        childResponse.EnsureSuccessStatusCode();
        var child = await childResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(child);

        var grandchild = await client.PostAsJsonAsync(
            "/v1/economy/categories",
            new AddCategoryRequest(household.Id.Value, "Grandchild", child.CategoryId, true));

        Assert.Equal(HttpStatusCode.BadRequest, grandchild.StatusCode);
    }

    [Fact]
    public async Task BudgetCopy_WhenNoPriorPeriod_ReturnsEmptyBudget()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);

        var copied = await client.PostAsJsonAsync(
            "/v1/economy/budgets/copy-from-previous",
            new CopyBudgetFromPreviousPeriodRequest(household.Id.Value, new DateOnly(2026, 6, 4)));

        Assert.Equal(HttpStatusCode.OK, copied.StatusCode);
        var budget = await copied.Content.ReadFromJsonAsync<BudgetResponse>();
        Assert.NotNull(budget);
        Assert.Empty(budget.Lines);
    }

    [Fact]
    public async Task BudgetCopy_WhenPriorPeriodExists_CopiesLines()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        var settings = await CreateSettingsAsync(client, household.Id.Value);
        var category = await AddBudgetCategoryAsync(client, household.Id.Value, "Groceries");

        var priorBudget = await CreateBudgetAsync(client, household.Id.Value, new DateOnly(2026, 5, 20));
        var upsert = await client.PutAsJsonAsync(
            "/v1/economy/budgets/lines",
            new UpsertBudgetLineRequest(
                household.Id.Value,
                priorBudget.BudgetId,
                category.CategoryId,
                new MoneyRequest(1200, settings.DefaultCurrency)));
        upsert.EnsureSuccessStatusCode();

        var copied = await client.PostAsJsonAsync(
            "/v1/economy/budgets/copy-from-previous",
            new CopyBudgetFromPreviousPeriodRequest(household.Id.Value, new DateOnly(2026, 6, 20)));

        Assert.Equal(HttpStatusCode.OK, copied.StatusCode);
        var budget = await copied.Content.ReadFromJsonAsync<BudgetResponse>();
        Assert.NotNull(budget);
        var line = Assert.Single(budget.Lines);
        Assert.Equal(category.CategoryId, line.CategoryId);
        Assert.Equal(1200, line.Amount.Amount);
    }

    private static async Task<CreateEconomySettingsResponse> CreateSettingsAsync(HttpClient client, Guid householdId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/economy/settings",
            new CreateEconomySettingsRequest(householdId, 1, "SEK"));
        response.EnsureSuccessStatusCode();
        var settings = await response.Content.ReadFromJsonAsync<CreateEconomySettingsResponse>();
        Assert.NotNull(settings);
        return settings;
    }

    private static async Task<CategoryResponse> AddBudgetCategoryAsync(HttpClient client, Guid householdId, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/economy/categories",
            new AddCategoryRequest(householdId, name, null, true));
        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(category);
        return category;
    }

    private static async Task<BudgetResponse> CreateBudgetAsync(HttpClient client, Guid householdId, DateOnly anchorDate)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/economy/budgets",
            new CreateBudgetRequest(householdId, anchorDate));
        response.EnsureSuccessStatusCode();
        var budget = await response.Content.ReadFromJsonAsync<BudgetResponse>();
        Assert.NotNull(budget);
        return budget;
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
