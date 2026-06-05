using System.Net;
using System.Net.Http.Json;
using Hemma.Modules.Economy.Features.AddCategory;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;
using Hemma.Modules.Economy.Features.CreateAccount;
using Hemma.Modules.Economy.Features.CreateBudget;
using Hemma.Modules.Economy.Features.CreateEconomySettings;
using Hemma.Modules.Economy.Features.CreateRecurringBill;
using Hemma.Modules.Economy.Features.CreateTransfer;
using Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;
using Hemma.Modules.Economy.Features.ConfirmEstimatedBill;
using Hemma.Modules.Economy.Features.GetAccountBalances;
using Hemma.Modules.Economy.Features.GetBudgetSummary;
using Hemma.Modules.Economy.Features.ListAccounts;
using Hemma.Modules.Economy.Features.ListCategories;
using Hemma.Modules.Economy.Features.ListRecurringBills;
using Hemma.Modules.Economy.Features.ListTransactions;
using Hemma.Modules.Economy.Features.RecordTransaction;
using Hemma.Modules.Economy.Features.SearchTransactionNote;
using Hemma.Modules.Economy.Features.UpsertBudgetLine;
using Hemma.Modules.Economy.Jobs;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

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

    [Fact]
    public async Task Transactions_CanRecordSearchAndFilterByReceipt()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var category = await AddBudgetCategoryAsync(client, household.Id.Value, "Groceries");

        var created = await client.PostAsJsonAsync(
            "/v1/economy/transactions",
            new RecordTransactionRequest(
                household.Id.Value,
                account.AccountId,
                category.CategoryId,
                new MoneyRequest(125, "SEK"),
                new DateOnly(2026, 6, 5),
                "ICA receipt",
                "Expense",
                ownerId));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var transaction = await created.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(transaction);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(household.Id.Value.ToString()), "householdId");
        var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "receipt.pdf");
        var attached = await client.PostAsync($"/v1/economy/transactions/{transaction.TransactionId}/receipt", form);
        Assert.Equal(HttpStatusCode.OK, attached.StatusCode);

        var withReceipt = await client.GetFromJsonAsync<ListTransactionsResponse>(
            $"/v1/economy/transactions?householdId={household.Id.Value}&hasReceipt=true");
        Assert.NotNull(withReceipt);
        Assert.Single(withReceipt.Transactions);

        var search = await client.GetFromJsonAsync<SearchTransactionNoteResponse>(
            $"/v1/economy/transactions/search?householdId={household.Id.Value}&search=receipt");
        Assert.NotNull(search);
        Assert.Single(search.Transactions);
    }

    [Fact]
    public async Task NeutralTransfer_ChangesBalancesWithoutBudgetActuals()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var checking = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var savings = await CreateAccountAsync(client, household.Id.Value, "Savings", "Savings", 0);
        var category = await AddBudgetCategoryAsync(client, household.Id.Value, "Transfer Bucket");
        var budget = await CreateBudgetAsync(client, household.Id.Value, new DateOnly(2026, 6, 5));
        var budgetLine = await client.PutAsJsonAsync(
            "/v1/economy/budgets/lines",
            new UpsertBudgetLineRequest(household.Id.Value, budget.BudgetId, category.CategoryId, new MoneyRequest(500, "SEK")));
        budgetLine.EnsureSuccessStatusCode();

        var transfer = await client.PostAsJsonAsync(
            "/v1/economy/transfers",
            new CreateTransferRequest(
                household.Id.Value,
                checking.AccountId,
                savings.AccountId,
                new MoneyRequest(250, "SEK"),
                new DateOnly(2026, 6, 5),
                "Move money",
                "Neutral",
                category.CategoryId,
                ownerId));
        Assert.True(
            transfer.StatusCode == HttpStatusCode.Created,
            await transfer.Content.ReadAsStringAsync());

        var balances = await client.GetFromJsonAsync<GetAccountBalancesResponse>(
            $"/v1/economy/accounts/balances?householdId={household.Id.Value}");
        Assert.NotNull(balances);
        Assert.Equal(750, balances.Accounts.Single(account => account.AccountId == checking.AccountId).Balance.Amount);
        Assert.Equal(250, balances.Accounts.Single(account => account.AccountId == savings.AccountId).Balance.Amount);

        var summary = await client.GetFromJsonAsync<GetBudgetSummaryResponse>(
            $"/v1/economy/budget-summary?householdId={household.Id.Value}&anchorDate=2026-06-05");
        Assert.NotNull(summary);
        Assert.Equal(0, summary.Lines.Single(line => line.CategoryId == category.CategoryId).Actual.Amount);
    }

    [Fact]
    public async Task SavingsTransfer_CountsAsBudgetActualAndCanBeOverPace()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var checking = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 10000);
        var savings = await CreateAccountAsync(client, household.Id.Value, "Savings", "Savings", 0);
        var category = await AddBudgetCategoryAsync(client, household.Id.Value, "Sparande");
        var budget = await CreateBudgetAsync(client, household.Id.Value, new DateOnly(2026, 6, 5));
        var budgetLine = await client.PutAsJsonAsync(
            "/v1/economy/budgets/lines",
            new UpsertBudgetLineRequest(household.Id.Value, budget.BudgetId, category.CategoryId, new MoneyRequest(6000, "SEK")));
        budgetLine.EnsureSuccessStatusCode();

        var transfer = await client.PostAsJsonAsync(
            "/v1/economy/transfers",
            new CreateTransferRequest(
                household.Id.Value,
                checking.AccountId,
                savings.AccountId,
                new MoneyRequest(5000, "SEK"),
                new DateOnly(2026, 6, 18),
                "Savings allocation",
                "Savings",
                category.CategoryId,
                ownerId));
        Assert.True(
            transfer.StatusCode == HttpStatusCode.Created,
            await transfer.Content.ReadAsStringAsync());

        var summary = await client.GetFromJsonAsync<GetBudgetSummaryResponse>(
            $"/v1/economy/budget-summary?householdId={household.Id.Value}&anchorDate=2026-06-18");
        Assert.NotNull(summary);
        var line = summary.Lines.Single(line => line.CategoryId == category.CategoryId);
        Assert.Equal(5000, line.Actual.Amount);
        Assert.True(line.IsOverPace);
    }

    [Fact]
    public async Task FixedRecurringBill_RunDueBills_PostsOneTransactionPerCycle()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var category = await AddBudgetCategoryAsync(client, household.Id.Value, "Software");
        var bill = await CreateRecurringBillAsync(
            client,
            household.Id.Value,
            account.AccountId,
            category.CategoryId,
            "Streaming",
            "Fixed",
            "Expense",
            119,
            new DateOnly(2026, 6, 1),
            5);

        var bus = fixture.Services.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(new RunDueBills(new DateOnly(2026, 6, 5)), CancellationToken.None);
        await bus.InvokeAsync(new RunDueBills(new DateOnly(2026, 6, 5)), CancellationToken.None);

        var transactions = await client.GetFromJsonAsync<ListTransactionsResponse>(
            $"/v1/economy/transactions?householdId={household.Id.Value}");
        Assert.NotNull(transactions);
        var transaction = Assert.Single(transactions.Transactions);
        Assert.Equal(119, transaction.Amount.Amount);
        Assert.False(transaction.IsPending);

        var listed = await client.GetFromJsonAsync<ListRecurringBillsResponse>(
            $"/v1/economy/recurring-bills?householdId={household.Id.Value}");
        Assert.NotNull(listed);
        Assert.Equal(new DateOnly(2026, 7, 5), listed.RecurringBills.Single(x => x.RecurringBillId == bill.RecurringBillId).NextDueOn);
    }

    [Fact]
    public async Task EstimatedRecurringBill_IsPendingUntilConfirmed()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var category = await AddBudgetCategoryAsync(client, household.Id.Value, "Utilities");
        var bill = await CreateRecurringBillAsync(
            client,
            household.Id.Value,
            account.AccountId,
            category.CategoryId,
            "Electricity",
            "Estimated",
            "Expense",
            500,
            new DateOnly(2026, 6, 1),
            5);

        var bus = fixture.Services.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(new RunDueBills(new DateOnly(2026, 6, 5)), CancellationToken.None);

        var pendingList = await client.GetFromJsonAsync<ListRecurringBillsResponse>(
            $"/v1/economy/recurring-bills?householdId={household.Id.Value}");
        Assert.NotNull(pendingList);
        var pending = Assert.Single(pendingList.RecurringBills.Single(x => x.RecurringBillId == bill.RecurringBillId).PendingOccurrences);
        Assert.NotNull(pending.TransactionId);

        var balancesBefore = await client.GetFromJsonAsync<GetAccountBalancesResponse>(
            $"/v1/economy/accounts/balances?householdId={household.Id.Value}");
        Assert.NotNull(balancesBefore);
        Assert.Equal(1000, balancesBefore.Accounts.Single(x => x.AccountId == account.AccountId).Balance.Amount);

        var confirmed = await client.PostAsJsonAsync(
            $"/v1/economy/recurring-bills/{bill.RecurringBillId}/confirm",
            new ConfirmEstimatedBillRequest(
                household.Id.Value,
                pending.TransactionId.Value,
                new MoneyRequest(650, "SEK"),
                new DateOnly(2026, 6, 6)));
        confirmed.EnsureSuccessStatusCode();
        var transaction = await confirmed.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(transaction);
        Assert.False(transaction.IsPending);
        Assert.Equal(650, transaction.Amount.Amount);

        var balancesAfter = await client.GetFromJsonAsync<GetAccountBalancesResponse>(
            $"/v1/economy/accounts/balances?householdId={household.Id.Value}");
        Assert.NotNull(balancesAfter);
        Assert.Equal(350, balancesAfter.Accounts.Single(x => x.AccountId == account.AccountId).Balance.Amount);
    }

    [Fact]
    public async Task SkippingOccurrence_DoesNotAffectLaterCycles()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var bill = await CreateRecurringBillAsync(
            client,
            household.Id.Value,
            account.AccountId,
            null,
            "Allowance",
            "Fixed",
            "Income",
            100,
            new DateOnly(2026, 6, 1),
            5);

        var skipped = await client.PostAsJsonAsync(
            $"/v1/economy/recurring-bills/{bill.RecurringBillId}/skip",
            new ChangeRecurringBillOccurrenceRequest(household.Id.Value, new DateOnly(2026, 6, 5)));
        skipped.EnsureSuccessStatusCode();

        var bus = fixture.Services.GetRequiredService<IMessageBus>();
        await bus.InvokeAsync(new RunDueBills(new DateOnly(2026, 6, 5)), CancellationToken.None);
        await bus.InvokeAsync(new RunDueBills(new DateOnly(2026, 7, 5)), CancellationToken.None);

        var transactions = await client.GetFromJsonAsync<ListTransactionsResponse>(
            $"/v1/economy/transactions?householdId={household.Id.Value}");
        Assert.NotNull(transactions);
        var transaction = Assert.Single(transactions.Transactions);
        Assert.Equal("Income", transaction.Kind);
        Assert.Equal(new DateOnly(2026, 7, 5), transaction.OccurredOn);
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

    private static async Task<AccountResponse> CreateAccountAsync(HttpClient client, Guid householdId, string name, string type, decimal openingBalance)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/economy/accounts",
            new CreateAccountRequest(householdId, name, type, new MoneyRequest(openingBalance, "SEK")));
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(account);
        return account;
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

    private static async Task<RecurringBillResponse> CreateRecurringBillAsync(
        HttpClient client,
        Guid householdId,
        Guid accountId,
        Guid? categoryId,
        string name,
        string type,
        string direction,
        decimal amount,
        DateOnly startsOn,
        int dayOfMonth)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/economy/recurring-bills",
            new CreateRecurringBillRequest(
                householdId,
                name,
                accountId,
                categoryId,
                new MoneyRequest(amount, "SEK"),
                type,
                direction,
                "Monthly",
                1,
                dayOfMonth,
                startsOn,
                null));
        response.EnsureSuccessStatusCode();
        var bill = await response.Content.ReadFromJsonAsync<RecurringBillResponse>();
        Assert.NotNull(bill);
        return bill;
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
