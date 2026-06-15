using System.Net;
using System.Net.Http.Json;
using Hemma.Modules.Audit.Persistence;
using Hemma.Modules.Economy.Contracts.Queries;
using Hemma.Modules.Economy.Features.AddCategory;
using Hemma.Modules.Economy.Features.Analytics;
using Hemma.Modules.Economy.Features.AssignTransactionToProject;
using Hemma.Modules.Economy.Features.CategorizationRules;
using Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;
using Hemma.Modules.Economy.Features.ConfirmEstimatedBill;
using Hemma.Modules.Economy.Features.Contracts;
using Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;
using Hemma.Modules.Economy.Features.CreateAccount;
using Hemma.Modules.Economy.Features.CreateBudget;
using Hemma.Modules.Economy.Features.CreateEconomySettings;
using Hemma.Modules.Economy.Features.CreateRecurringBill;
using Hemma.Modules.Economy.Features.CreateTransfer;
using Hemma.Modules.Economy.Features.Gdpr.Export;
using Hemma.Modules.Economy.Features.GetAccountBalances;
using Hemma.Modules.Economy.Features.GetBudgetSummary;
using Hemma.Modules.Economy.Features.GetEconomySettings;
using Hemma.Modules.Economy.Features.Import.CommitImport;
using Hemma.Modules.Economy.Features.Import.Contracts;
using Hemma.Modules.Economy.Features.Import.PreviewImport;
using Hemma.Modules.Economy.Features.ListAccounts;
using Hemma.Modules.Economy.Features.ListCategories;
using Hemma.Modules.Economy.Features.ListRecurringBills;
using Hemma.Modules.Economy.Features.ListTransactions;
using Hemma.Modules.Economy.Features.ListTransactionsForProject;
using Hemma.Modules.Economy.Features.NotificationPreferences;
using Hemma.Modules.Economy.Features.RecordTransaction;
using Hemma.Modules.Economy.Features.SearchTransactionNote;
using Hemma.Modules.Economy.Features.Subscriptions;
using Hemma.Modules.Economy.Features.UpsertBudgetLine;
using Hemma.Modules.Economy.Jobs;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Households.Contracts.Events;
using Hemma.Modules.Households.Domain;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Property.Contracts.Events;
using Hemma.Modules.Property.Features.CreateProject;
using Hemma.Modules.Property.Features.Shared;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Shared.Contracts;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Tracking;

namespace Hemma.Modules.Economy.IntegrationTests;

[Collection("EconomyModule")]
[Trait("Category", "Integration")]
public sealed class EconomyApiTests(EconomyApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SettingsRead_ReturnsNotFoundBeforeSetupAndSettingsAfterSetup()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var missing = await client.GetAsync($"/v1/economy/settings?householdId={household.Id.Value}");

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        await CreateSettingsAsync(client, household.Id.Value);

        var response = await client.GetAsync($"/v1/economy/settings?householdId={household.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<GetEconomySettingsResponse>();
        Assert.NotNull(settings);
        Assert.Equal(household.Id.Value, settings.HouseholdId);
        Assert.Equal(1, settings.CycleStartDay);
        Assert.Equal("SEK", settings.DefaultCurrency);
    }

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
        Assert.Contains(categories.Categories, category => string.Equals(category.Name, "Mat", StringComparison.Ordinal));
        Assert.Contains(categories.Categories, category => string.Equals(category.Name, "Sparande", StringComparison.Ordinal));
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
            new CreateAccountRequest(household.Id.Value, "Checking", "Spending", new MoneyDto(100, "SEK")));

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
    public async Task MoneyEndpoints_RejectNonSekCurrency()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "SEK Only", "sek-only");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);

        var accountResponse = await client.PostAsJsonAsync(
            "/v1/economy/accounts",
            new CreateAccountRequest(household.Id.Value, "Euro", "Spending", new MoneyDto(100, "EUR")));

        Assert.False(accountResponse.IsSuccessStatusCode);

        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 0);
        var transactionResponse = await client.PostAsJsonAsync(
            "/v1/economy/transactions",
            new RecordTransactionRequest(
                household.Id.Value,
                account.AccountId,
                CategoryId: null,
                new MoneyDto(100, "EUR"),
                new DateOnly(2026, 6, 5),
                "Euro transaction",
                "Expense",
                ownerId));

        Assert.False(transactionResponse.IsSuccessStatusCode);
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
    public async Task BudgetCreation_WhenPeriodAlreadyExists_ReturnsExistingBudget()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);

        var created = await client.PostAsJsonAsync(
            "/v1/economy/budgets",
            new CreateBudgetRequest(household.Id.Value, new DateOnly(2026, 6, 5)));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBudget = await created.Content.ReadFromJsonAsync<BudgetResponse>();
        Assert.NotNull(createdBudget);

        var existing = await client.PostAsJsonAsync(
            "/v1/economy/budgets",
            new CreateBudgetRequest(household.Id.Value, new DateOnly(2026, 6, 20)));

        Assert.Equal(HttpStatusCode.OK, existing.StatusCode);
        var existingBudget = await existing.Content.ReadFromJsonAsync<BudgetResponse>();
        Assert.NotNull(existingBudget);
        Assert.Equal(createdBudget.BudgetId, existingBudget.BudgetId);
        Assert.Equal(new DateOnly(2026, 6, 1), existingBudget.PeriodStartsOn);
        Assert.Empty(existingBudget.Lines);

        var summary = await client.GetFromJsonAsync<GetBudgetSummaryResponse>(
            $"/v1/economy/budget-summary?householdId={household.Id.Value}&anchorDate=2026-06-20");
        Assert.NotNull(summary);
        Assert.Equal(existingBudget.BudgetId, summary.BudgetId);
        Assert.Empty(summary.Lines);
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
                new MoneyDto(1200, settings.DefaultCurrency)));
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
                new MoneyDto(125, "SEK"),
                new DateOnly(2026, 6, 5),
                "ICA receipt",
                "Expense",
                ownerId));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var transaction = await created.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(transaction);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(household.Id.Value.ToString()), "householdId");
        var file = new ByteArrayContent("%PDF"u8.ToArray());
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
            new UpsertBudgetLineRequest(household.Id.Value, budget.BudgetId, category.CategoryId, new MoneyDto(500, "SEK")));
        budgetLine.EnsureSuccessStatusCode();

        var transfer = await client.PostAsJsonAsync(
            "/v1/economy/transfers",
            new CreateTransferRequest(
                household.Id.Value,
                checking.AccountId,
                savings.AccountId,
                new MoneyDto(250, "SEK"),
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
            new UpsertBudgetLineRequest(household.Id.Value, budget.BudgetId, category.CategoryId, new MoneyDto(6000, "SEK")));
        budgetLine.EnsureSuccessStatusCode();

        var transfer = await client.PostAsJsonAsync(
            "/v1/economy/transfers",
            new CreateTransferRequest(
                household.Id.Value,
                checking.AccountId,
                savings.AccountId,
                new MoneyDto(5000, "SEK"),
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
                new MoneyDto(650, "SEK"),
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

    [Fact]
    public async Task ImportPreview_ReturnsRowErrorsAndAppliesCategorizationRules()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var food = await AddBudgetCategoryAsync(client, household.Id.Value, "Food");

        var ruleResponse = await client.PostAsJsonAsync(
            "/v1/economy/categorization-rules",
            new CategorizationRuleRequest(household.Id.Value, "Contains", "ICA", food.CategoryId));
        ruleResponse.EnsureSuccessStatusCode();

        var preview = await client.PostAsJsonAsync(
            "/v1/economy/import/preview",
            new PreviewImportRequest(
                household.Id.Value,
                account.AccountId,
                [
                    new NormalizedImportRowRequest(1, new DateOnly(2026, 6, 5), -123.45m, "ICA Kvantum", "SEK", null, "A1", null, null, null),
                    new NormalizedImportRowRequest(2, null, null, "", null, null, null, null, null, null)
                ]));

        preview.EnsureSuccessStatusCode();
        var body = await preview.Content.ReadFromJsonAsync<PreviewImportResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.PreviewFingerprint);
        Assert.Equal(food.CategoryId, body.Rows[0].SuggestedCategoryId);
        Assert.Equal("None", body.Rows[0].DuplicateState);
        Assert.NotEmpty(body.Rows[1].Errors);
    }

    [Fact]
    public async Task ImportCommit_RejectsChangedFingerprintAndReimportFlagsDuplicates()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var category = await AddBudgetCategoryAsync(client, household.Id.Value, "Groceries");
        var rows = new[]
        {
            new NormalizedImportRowRequest(1, new DateOnly(2026, 6, 5), -99m, "ICA", "SEK", null, "A1", null, null, category.CategoryId),
            new NormalizedImportRowRequest(2, new DateOnly(2026, 6, 6), 250m, "Salary", "SEK", null, "B1", null, null, null)
        };

        var previewResponse = await client.PostAsJsonAsync(
            "/v1/economy/import/preview",
            new PreviewImportRequest(household.Id.Value, account.AccountId, rows));
        previewResponse.EnsureSuccessStatusCode();
        var preview = await previewResponse.Content.ReadFromJsonAsync<PreviewImportResponse>();
        Assert.NotNull(preview);

        var changedRows = rows
            .Select((row, index) => index == 0 ? row with { Description = "Changed" } : row)
            .ToArray();
        var changedCommit = await client.PostAsJsonAsync(
            "/v1/economy/import/commit",
            new CommitImportRequest(household.Id.Value, account.AccountId, preview.PreviewFingerprint, changedRows));
        Assert.Equal(HttpStatusCode.Conflict, changedCommit.StatusCode);

        var committed = await client.PostAsJsonAsync(
            "/v1/economy/import/commit",
            new CommitImportRequest(household.Id.Value, account.AccountId, preview.PreviewFingerprint, rows));
        committed.EnsureSuccessStatusCode();
        var commitBody = await committed.Content.ReadFromJsonAsync<CommitImportResponse>();
        Assert.NotNull(commitBody);
        Assert.Equal(2, commitBody.ImportedCount);
        Assert.Single(commitBody.SuggestedRules);

        var reimportPreviewResponse = await client.PostAsJsonAsync(
            "/v1/economy/import/preview",
            new PreviewImportRequest(household.Id.Value, account.AccountId, rows));
        reimportPreviewResponse.EnsureSuccessStatusCode();
        var reimportPreview = await reimportPreviewResponse.Content.ReadFromJsonAsync<PreviewImportResponse>();
        Assert.NotNull(reimportPreview);
        Assert.All(reimportPreview.Rows, row => Assert.Equal("Exact", row.DuplicateState));

        var reimportCommit = await client.PostAsJsonAsync(
            "/v1/economy/import/commit",
            new CommitImportRequest(household.Id.Value, account.AccountId, reimportPreview.PreviewFingerprint, rows));
        reimportCommit.EnsureSuccessStatusCode();
        var reimportBody = await reimportCommit.Content.ReadFromJsonAsync<CommitImportResponse>();
        Assert.NotNull(reimportBody);
        Assert.Equal(0, reimportBody.ImportedCount);
        Assert.Equal(2, reimportBody.DuplicateCount);
    }

    [Fact]
    public async Task SubscriptionLifecycle_DoesNotPostTransactionsAndDerivesPriceHistory()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);

        var subscription = await CreateSubscriptionAsync(
            client,
            household.Id.Value,
            account.AccountId,
            "Spotify",
            119,
            15,
            new DateOnly(2026, 1, 15));

        var emptyTransactions = await client.GetFromJsonAsync<ListTransactionsResponse>(
            $"/v1/economy/transactions?householdId={household.Id.Value}");
        Assert.NotNull(emptyTransactions);
        Assert.Empty(emptyTransactions.Transactions);

        var first = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 99, new DateOnly(2026, 2, 15), "Spotify");
        var second = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 119, new DateOnly(2026, 3, 15), "Spotify");
        await LinkSubscriptionAsync(client, household.Id.Value, subscription.SubscriptionId, first.TransactionId);
        await LinkSubscriptionAsync(client, household.Id.Value, subscription.SubscriptionId, second.TransactionId);

        var history = await client.GetFromJsonAsync<ChargeHistoryResponse>(
            $"/v1/economy/subscriptions/{subscription.SubscriptionId}/charge-history?householdId={household.Id.Value}");
        Assert.NotNull(history);
        Assert.Equal(2, history.Charges.Count);
        var priceChange = Assert.Single(history.PriceChanges);
        Assert.Equal(99, priceChange.PreviousAmount.Amount);
        Assert.Equal(119, priceChange.NewAmount.Amount);
    }

    [Fact]
    public async Task SubscriptionMonthCalendar_ReturnsPredictedAndActualChargesForSelectedMonth()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var subscription = await CreateSubscriptionAsync(
            client,
            household.Id.Value,
            account.AccountId,
            "Spotify",
            119,
            15,
            new DateOnly(2026, 1, 15));
        var transaction = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 119, new DateOnly(2026, 3, 15), "Spotify");
        await LinkSubscriptionAsync(client, household.Id.Value, subscription.SubscriptionId, transaction.TransactionId);

        var march = await client.GetFromJsonAsync<MonthChargeCalendarResponse>(
            $"/v1/economy/subscriptions/month-calendar?householdId={household.Id.Value}&month=2026-03-01");

        Assert.NotNull(march);
        var day = Assert.Single(march.Days);
        Assert.Equal(new DateOnly(2026, 3, 15), day.Date);
        var charge = Assert.Single(day.Charges);
        Assert.Equal("actual", charge.MatchState);
        Assert.Equal(transaction.TransactionId, charge.TransactionId);
    }

    [Fact]
    public async Task ListSubscriptions_ReturnsHouseholdSubscriptionsIncludingCancelled()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var spotify = await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Spotify", 119, 15, new DateOnly(2026, 1, 15));
        var netflix = await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Netflix", 139, 5, new DateOnly(2026, 1, 5));

        fixture.Clock.Set(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));
        var cancel = await client.PutAsJsonAsync(
            $"/v1/economy/subscriptions/{netflix.SubscriptionId}/state",
            new ChangeLifecycleStateRequest(household.Id.Value, "Cancelled", null));
        cancel.EnsureSuccessStatusCode();
        var cancelledBody = await cancel.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(cancelledBody);
        Assert.Equal(new DateOnly(2026, 6, 10), cancelledBody.CancelledOn);

        var list = await client.GetFromJsonAsync<ListSubscriptionsResponse>(
            $"/v1/economy/subscriptions?householdId={household.Id.Value}");

        Assert.NotNull(list);
        Assert.Equal(2, list.Subscriptions.Count);
        var cancelled = list.Subscriptions.Single(x => x.SubscriptionId == netflix.SubscriptionId);
        Assert.Equal("Cancelled", cancelled.LifecycleState);
        Assert.Equal(new DateOnly(2026, 6, 10), cancelled.CancelledOn);
        var active = list.Subscriptions.Single(x => x.SubscriptionId == spotify.SubscriptionId);
        Assert.Equal("Active", active.LifecycleState);
        Assert.Null(active.CancelledOn);
    }

    [Fact]
    public async Task SubscriptionLinkCandidates_ReturnsUnlinkedMatchingTransactionsOnly()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var subscription = await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Spotify", 119, 15, new DateOnly(2026, 1, 15));

        var match = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 119, new DateOnly(2026, 3, 13), "Spotify AB");
        await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 119, new DateOnly(2026, 3, 15), "Groceries");
        var alreadyLinked = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 119, new DateOnly(2026, 4, 15), "Spotify AB");
        await LinkSubscriptionAsync(client, household.Id.Value, subscription.SubscriptionId, alreadyLinked.TransactionId);

        fixture.Clock.Set(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));
        var response = await client.GetFromJsonAsync<LinkCandidatesResponse>(
            $"/v1/economy/subscriptions/{subscription.SubscriptionId}/link-candidates?householdId={household.Id.Value}");

        Assert.NotNull(response);
        var candidate = Assert.Single(response.Candidates);
        Assert.Equal(match.TransactionId, candidate.TransactionId);
        Assert.Equal(new DateOnly(2026, 3, 13), candidate.OccurredOn);
        Assert.Equal(119, candidate.Amount.Amount);
    }

    [Fact]
    public async Task LinkTransaction_WhenAlreadyLinkedToAnotherSubscription_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var spotify = await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Spotify", 119, 15, new DateOnly(2026, 1, 15));
        var netflix = await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Netflix", 139, 5, new DateOnly(2026, 1, 5));
        var transaction = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 119, new DateOnly(2026, 3, 15), "Spotify");

        var linked = await client.PostAsJsonAsync(
            $"/v1/economy/subscriptions/{spotify.SubscriptionId}/link",
            new LinkTransactionRequest(household.Id.Value, transaction.TransactionId));
        linked.EnsureSuccessStatusCode();
        var linkedBody = await linked.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(linkedBody);
        Assert.Equal(spotify.SubscriptionId, linkedBody.SubscriptionId);

        var conflict = await client.PostAsJsonAsync(
            $"/v1/economy/subscriptions/{netflix.SubscriptionId}/link",
            new LinkTransactionRequest(household.Id.Value, transaction.TransactionId));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        var idempotent = await client.PostAsJsonAsync(
            $"/v1/economy/subscriptions/{spotify.SubscriptionId}/link",
            new LinkTransactionRequest(household.Id.Value, transaction.TransactionId));
        Assert.Equal(HttpStatusCode.OK, idempotent.StatusCode);
    }

    [Fact]
    public async Task GetSubscription_ReturnsSubscriptionAndNotFoundForUnknownId()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var created = await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Spotify", 119, 15, new DateOnly(2026, 1, 15));

        var fetched = await client.GetFromJsonAsync<SubscriptionResponse>(
            $"/v1/economy/subscriptions/{created.SubscriptionId}?householdId={household.Id.Value}");
        Assert.NotNull(fetched);
        Assert.Equal(created.SubscriptionId, fetched.SubscriptionId);
        Assert.Equal("Spotify", fetched.Name);
        Assert.Equal(119, fetched.ExpectedAmount.Amount);

        var missing = await client.GetAsync(
            $"/v1/economy/subscriptions/{Guid.NewGuid()}?householdId={household.Id.Value}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task SubscriptionMonthCalendar_ShowsEarlyActualChargeOnItsActualDay()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var subscription = await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Spotify", 119, 15, new DateOnly(2026, 1, 15));
        var transaction = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 119, new DateOnly(2026, 3, 13), "Spotify");
        await LinkSubscriptionAsync(client, household.Id.Value, subscription.SubscriptionId, transaction.TransactionId);

        var march = await client.GetFromJsonAsync<MonthChargeCalendarResponse>(
            $"/v1/economy/subscriptions/month-calendar?householdId={household.Id.Value}&month=2026-03-01");

        Assert.NotNull(march);
        var day = Assert.Single(march.Days);
        Assert.Equal(new DateOnly(2026, 3, 13), day.Date);
        var charge = Assert.Single(day.Charges);
        Assert.Equal("actual", charge.MatchState);
        Assert.Equal(transaction.TransactionId, charge.TransactionId);
        Assert.Equal(119, march.ActualTotal.Amount);
        Assert.Equal(0, march.PredictedTotal.Amount);
    }

    [Fact]
    public async Task SubscriptionMonthCalendar_ReturnsActualAndPredictedTotals()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var spotify = await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Spotify", 119, 15, new DateOnly(2026, 1, 15));
        await CreateSubscriptionAsync(client, household.Id.Value, account.AccountId, "Netflix", 139, 5, new DateOnly(2026, 1, 5));
        var transaction = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 119, new DateOnly(2026, 3, 15), "Spotify");
        await LinkSubscriptionAsync(client, household.Id.Value, spotify.SubscriptionId, transaction.TransactionId);

        var march = await client.GetFromJsonAsync<MonthChargeCalendarResponse>(
            $"/v1/economy/subscriptions/month-calendar?householdId={household.Id.Value}&month=2026-03-01");

        Assert.NotNull(march);
        Assert.Equal(119, march.ActualTotal.Amount);
        Assert.Equal("SEK", march.ActualTotal.Currency);
        Assert.Equal(139, march.PredictedTotal.Amount);
        Assert.Equal("SEK", march.PredictedTotal.Currency);
    }

    [Fact]
    public async Task ImportPreview_SuggestsSubscriptionMatchWithoutLinking()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var subscription = await CreateSubscriptionAsync(
            client,
            household.Id.Value,
            account.AccountId,
            "Spotify",
            119,
            15,
            new DateOnly(2026, 1, 15));

        var preview = await client.PostAsJsonAsync(
            "/v1/economy/import/preview",
            new PreviewImportRequest(
                household.Id.Value,
                account.AccountId,
                [new NormalizedImportRowRequest(1, new DateOnly(2026, 3, 16), -119m, "Spotify AB", "SEK", null, null, null, null, null)]));

        preview.EnsureSuccessStatusCode();
        var body = await preview.Content.ReadFromJsonAsync<PreviewImportResponse>();
        Assert.NotNull(body);
        var suggestion = Assert.Single(body.Rows[0].SuggestedSubscriptionMatches);
        Assert.Equal(subscription.SubscriptionId, suggestion.SubscriptionId);
        Assert.Equal("suggested", suggestion.MatchState);
    }

    [Fact]
    public async Task Analytics_ReturnsEmptySeriesForEmptyDataset()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);

        var trend = await client.GetFromJsonAsync<GetCategoryTrendResponse>(
            $"/v1/economy/analytics/category-trend?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");
        var breakdown = await client.GetFromJsonAsync<GetSpendBreakdownResponse>(
            $"/v1/economy/analytics/spend-breakdown?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");
        var incomeVsExpense = await client.GetFromJsonAsync<GetIncomeVsExpenseResponse>(
            $"/v1/economy/analytics/income-vs-expense?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");
        var variance = await client.GetFromJsonAsync<GetVarianceHistoryResponse>(
            $"/v1/economy/analytics/variance-history?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");
        var top = await client.GetFromJsonAsync<GetTopTransactionsResponse>(
            $"/v1/economy/analytics/top-transactions?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");

        Assert.NotNull(trend);
        Assert.NotNull(breakdown);
        Assert.NotNull(incomeVsExpense);
        Assert.NotNull(variance);
        Assert.NotNull(top);
        Assert.Empty(trend.Series);
        Assert.Empty(breakdown.Slices);
        Assert.Empty(incomeVsExpense.Series);
        Assert.Empty(variance.Series);
        Assert.Empty(top.Transactions);
    }

    [Fact]
    public async Task Analytics_ExcludesTransfersExceptSavingsAllocationBreakdown()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var checking = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 10000);
        var savings = await CreateAccountAsync(client, household.Id.Value, "Savings", "Savings", 0);
        var groceries = await AddBudgetCategoryAsync(client, household.Id.Value, "Groceries");
        var savingsCategory = await AddBudgetCategoryAsync(client, household.Id.Value, "Savings Allocation");
        var budget = await CreateBudgetAsync(client, household.Id.Value, new DateOnly(2026, 6, 5));
        var groceryLine = await client.PutAsJsonAsync(
            "/v1/economy/budgets/lines",
            new UpsertBudgetLineRequest(household.Id.Value, budget.BudgetId, groceries.CategoryId, new MoneyDto(1000, "SEK")));
        groceryLine.EnsureSuccessStatusCode();
        var savingsLine = await client.PutAsJsonAsync(
            "/v1/economy/budgets/lines",
            new UpsertBudgetLineRequest(household.Id.Value, budget.BudgetId, savingsCategory.CategoryId, new MoneyDto(5000, "SEK")));
        savingsLine.EnsureSuccessStatusCode();

        await RecordTransactionAsync(client, household.Id.Value, checking.AccountId, groceries.CategoryId, 300, new DateOnly(2026, 6, 5), "ICA", "Expense");
        await RecordTransactionAsync(client, household.Id.Value, checking.AccountId, null, 9000, new DateOnly(2026, 6, 25), "Salary", "Income");
        var neutral = await client.PostAsJsonAsync(
            "/v1/economy/transfers",
            new CreateTransferRequest(
                household.Id.Value,
                checking.AccountId,
                savings.AccountId,
                new MoneyDto(1000, "SEK"),
                new DateOnly(2026, 6, 10),
                "Neutral transfer",
                "Neutral",
                savingsCategory.CategoryId,
                ownerId));
        neutral.EnsureSuccessStatusCode();
        var savingsTransfer = await client.PostAsJsonAsync(
            "/v1/economy/transfers",
            new CreateTransferRequest(
                household.Id.Value,
                checking.AccountId,
                savings.AccountId,
                new MoneyDto(5000, "SEK"),
                new DateOnly(2026, 6, 18),
                "Savings transfer",
                "Savings",
                savingsCategory.CategoryId,
                ownerId));
        savingsTransfer.EnsureSuccessStatusCode();

        var trend = await client.GetFromJsonAsync<GetCategoryTrendResponse>(
            $"/v1/economy/analytics/category-trend?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");
        var breakdown = await client.GetFromJsonAsync<GetSpendBreakdownResponse>(
            $"/v1/economy/analytics/spend-breakdown?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");
        var incomeVsExpense = await client.GetFromJsonAsync<GetIncomeVsExpenseResponse>(
            $"/v1/economy/analytics/income-vs-expense?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");
        var variance = await client.GetFromJsonAsync<GetVarianceHistoryResponse>(
            $"/v1/economy/analytics/variance-history?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30");
        var top = await client.GetFromJsonAsync<GetTopTransactionsResponse>(
            $"/v1/economy/analytics/top-transactions?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30&limit=5");

        Assert.NotNull(trend);
        var trendSeries = Assert.Single(trend.Series);
        Assert.Equal(groceries.CategoryId, trendSeries.CategoryId);
        Assert.Equal(300, Assert.Single(trendSeries.Points).Value.Amount);

        Assert.NotNull(breakdown);
        Assert.Equal(2, breakdown.Slices.Count);
        Assert.Equal(300, breakdown.Slices.Single(slice => slice.CategoryId == groceries.CategoryId).Value.Amount);
        Assert.Equal(5000, breakdown.Slices.Single(slice => slice.CategoryId == savingsCategory.CategoryId).Value.Amount);

        Assert.NotNull(incomeVsExpense);
        var point = Assert.Single(incomeVsExpense.Series);
        Assert.Equal("2026-06", point.Label);
        Assert.Equal(9000, point.Income.Amount);
        Assert.Equal(300, point.Expense.Amount);
        Assert.Equal(8700, point.Net.Amount);

        Assert.NotNull(variance);
        var variancePoint = Assert.Single(variance.Series);
        Assert.Equal(6000, variancePoint.Planned.Amount);
        Assert.Equal(5300, variancePoint.Actual.Amount);
        Assert.Equal(700, variancePoint.Variance.Amount);

        Assert.NotNull(top);
        Assert.DoesNotContain(top.Transactions, transaction => string.Equals(transaction.Kind, "Transfer", StringComparison.Ordinal));
        Assert.Equal(9000, top.Transactions.First().Amount.Amount);
    }

    [Fact]
    public async Task Analytics_VarianceHistory_WhenCategoryIdIsProvided_ReturnsCategoryVarianceOnly()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var checking = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 10000);
        var electricity = await AddBudgetCategoryAsync(client, household.Id.Value, "Electricity");
        var groceries = await AddBudgetCategoryAsync(client, household.Id.Value, "Groceries");
        var unbudgeted = await AddBudgetCategoryAsync(client, household.Id.Value, "Dining out");
        var budget = await CreateBudgetAsync(client, household.Id.Value, new DateOnly(2026, 6, 5));

        var electricityLine = await client.PutAsJsonAsync(
            "/v1/economy/budgets/lines",
            new UpsertBudgetLineRequest(household.Id.Value, budget.BudgetId, electricity.CategoryId, new MoneyDto(1200, "SEK")));
        electricityLine.EnsureSuccessStatusCode();
        var groceryLine = await client.PutAsJsonAsync(
            "/v1/economy/budgets/lines",
            new UpsertBudgetLineRequest(household.Id.Value, budget.BudgetId, groceries.CategoryId, new MoneyDto(3000, "SEK")));
        groceryLine.EnsureSuccessStatusCode();

        await RecordTransactionAsync(client, household.Id.Value, checking.AccountId, electricity.CategoryId, 1500, new DateOnly(2026, 6, 10), "Power company", "Expense");
        await RecordTransactionAsync(client, household.Id.Value, checking.AccountId, groceries.CategoryId, 800, new DateOnly(2026, 6, 11), "Market", "Expense");

        var filtered = await client.GetFromJsonAsync<GetVarianceHistoryResponse>(
            $"/v1/economy/analytics/variance-history?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30&categoryId={electricity.CategoryId}");
        var empty = await client.GetFromJsonAsync<GetVarianceHistoryResponse>(
            $"/v1/economy/analytics/variance-history?householdId={household.Id.Value}&from=2026-06-01&to=2026-06-30&categoryId={unbudgeted.CategoryId}");

        Assert.NotNull(filtered);
        var point = Assert.Single(filtered.Series);
        Assert.Equal("2026-06", point.Label);
        Assert.Equal(1200, point.Planned.Amount);
        Assert.Equal(1500, point.Actual.Amount);
        Assert.Equal(-300, point.Variance.Amount);

        Assert.NotNull(empty);
        Assert.Empty(empty.Series);
    }

    [Fact]
    public async Task Analytics_PeriodComparison_ComparesCurrentAndPreviousPeriods()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "acme");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var checking = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 10000);
        var groceries = await AddBudgetCategoryAsync(client, household.Id.Value, "Groceries");
        await RecordTransactionAsync(client, household.Id.Value, checking.AccountId, groceries.CategoryId, 200, new DateOnly(2026, 5, 20), "May", "Expense");
        await RecordTransactionAsync(client, household.Id.Value, checking.AccountId, groceries.CategoryId, 300, new DateOnly(2026, 6, 20), "June", "Expense");

        var comparison = await client.GetFromJsonAsync<GetPeriodComparisonResponse>(
            $"/v1/economy/analytics/period-comparison?householdId={household.Id.Value}&anchorDate=2026-06-20");

        Assert.NotNull(comparison);
        Assert.Equal(new DateOnly(2026, 6, 1), comparison.CurrentPeriodStartsOn);
        Assert.Equal(new DateOnly(2026, 5, 1), comparison.PreviousPeriodStartsOn);
        var spend = Assert.Single(comparison.Series);
        Assert.Equal("spend", spend.Label);
        Assert.Equal(300, spend.Current.Amount);
        Assert.Equal(200, spend.Previous.Amount);
        Assert.Equal(100, spend.Delta.Amount);
        Assert.Equal(50, spend.DeltaPercent);
    }

    [Fact]
    public async Task GdprExport_ReturnsOnlyCallerPersonalEconomyData()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Gdpr Export", "gdpr-export");
        await AddHouseholdMemberAsync(household.Id, memberId);
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 0);
        var category = await AddBudgetCategoryAsync(client, household.Id.Value, "Groceries");

        await RecordTransactionAsync(
            client,
            household.Id.Value,
            account.AccountId,
            category.CategoryId,
            45,
            new DateOnly(2026, 6, 5),
            "Market",
            "Expense",
            ownerId);
        await RecordTransactionAsync(
            client,
            household.Id.Value,
            account.AccountId,
            category.CategoryId,
            95,
            new DateOnly(2026, 6, 6),
            "Other member private note",
            "Expense",
            memberId);

        var response = await client.GetAsync($"/v1/economy/gdpr/export?householdId={household.Id.Value}");
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseBody);
        var export = await response.Content.ReadFromJsonAsync<ExportEconomyGdprResponse>();

        Assert.NotNull(export);
        Assert.Equal(household.Id.Value, export.HouseholdId);
        Assert.True(export.Data.ContainsKey("transactions"));
        Assert.Contains("Market", responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Other member private note", responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain(memberId.ToString(), responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("households", responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EconomyMember_CanReadButCannotMutateHouseholdEconomy()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Readonly Economy", "readonly-economy");
        await AddHouseholdMemberAsync(household.Id, memberId);
        using var ownerClient = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        using var memberClient = fixture.CreateAuthenticatedClient(memberId, "member@example.com", "Member");
        await CreateSettingsAsync(ownerClient, household.Id.Value);

        var read = await memberClient.GetAsync($"/v1/economy/settings?householdId={household.Id.Value}");
        var write = await memberClient.PostAsJsonAsync(
            "/v1/economy/accounts",
            new CreateAccountRequest(household.Id.Value, "Member Checking", "Spending", new MoneyDto(100, "SEK")));

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task EconomyMutation_CreatesAuditEntry()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Audit Economy", "audit-economy");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);

        HttpResponseMessage? response = null;
        Task CreateAccountForAuditAsync(IMessageContext _) =>
            client.PostAsJsonAsync(
                    "/v1/economy/accounts",
                    new CreateAccountRequest(household.Id.Value, "Audit Checking", "Spending", new MoneyDto(100, "SEK")))
                .ContinueWith(task => response = task.Result);

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .ExecuteAndWaitAsync(CreateAccountForAuditAsync);

        Assert.NotNull(response);
        response.EnsureSuccessStatusCode();

        var auditEntry = await fixture.QueryDbAsync<AuditDbContext, (string? ResourceType, Guid? ActorId)>((db, ct) =>
            db.AuditEntries
                .Where(entry => entry.HouseholdId == household.Id.Value &&
                    entry.EventType == "economy.account.created")
                .Select(entry => new ValueTuple<string?, Guid?>(entry.ResourceType, entry.ActorId))
                .SingleAsync(ct));

        Assert.Equal("Account", auditEntry.ResourceType);
        Assert.Equal(ownerId, auditEntry.ActorId);
    }

    [Fact]
    public async Task NotificationPreferences_CanReadDefaultsAndUpdate()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Prefs", "prefs");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");

        var defaults = await client.GetFromJsonAsync<EconomyNotificationPreferencesResponse>(
            $"/v1/economy/notification-preferences?householdId={household.Id.Value}");

        Assert.NotNull(defaults);
        Assert.True(defaults.BudgetAlertsEnabled);
        Assert.True(defaults.BillAlertsEnabled);
        Assert.True(defaults.TrialAlertsEnabled);

        var update = await client.PutAsJsonAsync(
            "/v1/economy/notification-preferences",
            new UpdateEconomyNotificationPreferencesRequest(
                household.Id.Value,
                BudgetAlertsEnabled: false,
                BillAlertsEnabled: true,
                TrialAlertsEnabled: false));
        update.EnsureSuccessStatusCode();

        var updated = await update.Content.ReadFromJsonAsync<EconomyNotificationPreferencesResponse>();

        Assert.NotNull(updated);
        Assert.False(updated.BudgetAlertsEnabled);
        Assert.True(updated.BillAlertsEnabled);
        Assert.False(updated.TrialAlertsEnabled);
    }

    [Fact]
    public async Task UserErasureRequested_ClearsPayerAndReceipt()
    {
        var ownerId = Guid.NewGuid();
        var erasedUserId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Gdpr Erase", "gdpr-erase");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 0);
        var transaction = await RecordTransactionAsync(
            client,
            household.Id.Value,
            account.AccountId,
            null,
            60,
            new DateOnly(2026, 6, 5),
            "Receipt transaction",
            "Expense",
            erasedUserId);
        await AttachReceiptAsync(client, household.Id.Value, transaction.TransactionId);

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .InvokeMessageAndWaitAsync(new UserErasureRequestedV1(erasedUserId, "Removed", Guid.NewGuid()));

        var erased = await fixture.QueryDbAsync<EconomyDbContext, (Guid? PayerId, string? Container, string? Key)>((db, ct) =>
            db.Transactions
                .Where(t => t.HouseholdId == household.Id.Value)
                .Select(t => new { t.Id, t.PayerId, t.ReceiptBlobContainer, t.ReceiptBlobKey })
                .ToListAsync(ct)
                .ContinueWith(task =>
                {
                    var found = task.Result.Single(t => t.Id.Value == transaction.TransactionId);
                    return (found.PayerId, found.ReceiptBlobContainer, found.ReceiptBlobKey);
                }, ct));

        Assert.Null(erased.PayerId);
        Assert.Null(erased.Container);
        Assert.Null(erased.Key);
    }

    [Fact]
    public async Task HouseholdMemberRemoved_ClearsMemberEconomyDataForThatHouseholdOnly()
    {
        var ownerId = Guid.NewGuid();
        var removedUserId = Guid.NewGuid();
        var retainedUserId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Removed Member", "removed-member");
        var otherHousehold = await CreateHouseholdAsync(ownerId, "Other Member", "other-member");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        await CreateSettingsAsync(client, otherHousehold.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 0);
        var otherAccount = await CreateAccountAsync(client, otherHousehold.Id.Value, "Other", "Spending", 0);
        var removedTransaction = await RecordTransactionAsync(
            client,
            household.Id.Value,
            account.AccountId,
            null,
            20,
            new DateOnly(2026, 6, 5),
            "Removed member",
            "Expense",
            removedUserId);
        await AttachReceiptAsync(client, household.Id.Value, removedTransaction.TransactionId);
        var retainedSameHouseholdTransaction = await RecordTransactionAsync(
            client,
            household.Id.Value,
            account.AccountId,
            null,
            25,
            new DateOnly(2026, 6, 6),
            "Retained same household",
            "Expense",
            retainedUserId);
        await AttachReceiptAsync(client, household.Id.Value, retainedSameHouseholdTransaction.TransactionId);
        var retainedTransaction = await RecordTransactionAsync(
            client,
            otherHousehold.Id.Value,
            otherAccount.AccountId,
            null,
            30,
            new DateOnly(2026, 6, 5),
            "Other household",
            "Expense",
            removedUserId);

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .InvokeMessageAndWaitAsync(new HouseholdMemberRemovedV1(
                household.Id.Value,
                removedUserId,
                ownerId,
                Guid.NewGuid()));

        var transactionIds = new[]
        {
            removedTransaction.TransactionId,
            retainedSameHouseholdTransaction.TransactionId,
            retainedTransaction.TransactionId
        };
        var states = await fixture.QueryDbAsync<EconomyDbContext, Dictionary<Guid, (Guid? PayerId, string? Note, bool HasReceipt)>>((db, ct) =>
            db.Transactions
                .Where(t => t.HouseholdId == household.Id.Value || t.HouseholdId == otherHousehold.Id.Value)
                .Select(t => new { t.Id, t.PayerId, t.Note, t.ReceiptBlobContainer, t.ReceiptBlobKey })
                .ToListAsync(ct)
                .ContinueWith(task => task.Result
                    .Where(t => transactionIds.Contains(t.Id.Value))
                    .ToDictionary(
                        t => t.Id.Value,
                        t => (t.PayerId, t.Note, t.ReceiptBlobContainer is not null && t.ReceiptBlobKey is not null)),
                    ct));

        Assert.Null(states[removedTransaction.TransactionId].PayerId);
        Assert.Null(states[removedTransaction.TransactionId].Note);
        Assert.False(states[removedTransaction.TransactionId].HasReceipt);
        Assert.Equal(retainedUserId, states[retainedSameHouseholdTransaction.TransactionId].PayerId);
        Assert.Equal("Retained same household", states[retainedSameHouseholdTransaction.TransactionId].Note);
        Assert.True(states[retainedSameHouseholdTransaction.TransactionId].HasReceipt);
        Assert.Equal(removedUserId, states[retainedTransaction.TransactionId].PayerId);
        Assert.Equal("Other household", states[retainedTransaction.TransactionId].Note);
    }

    [Fact]
    public async Task Transactions_CanAssignAndClearProjectLink()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "assign-project");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var transaction = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 250, new DateOnly(2026, 6, 5), "Tiles");
        var project = await CreateProjectAsync(client, household.Id.Value, "Kitchen");
        var projectId = project.ProjectId;

        var assigned = await AssignTransactionToProjectAsync(client, household.Id.Value, transaction.TransactionId, projectId);
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);

        var linked = await client.GetFromJsonAsync<ListTransactionsForProjectResponse>(
            $"/v1/economy/projects/{projectId}/transactions?householdId={household.Id.Value}");
        Assert.NotNull(linked);
        Assert.Equal(transaction.TransactionId, Assert.Single(linked.Transactions).TransactionId);

        var cleared = await AssignTransactionToProjectAsync(client, household.Id.Value, transaction.TransactionId, projectId: null);
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);

        var afterClear = await client.GetFromJsonAsync<ListTransactionsForProjectResponse>(
            $"/v1/economy/projects/{projectId}/transactions?householdId={household.Id.Value}");
        Assert.NotNull(afterClear);
        Assert.Empty(afterClear.Transactions);
    }

    [Fact]
    public async Task ProjectSpendSummary_AggregatesLinkedTransactions()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "spend-summary");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 5000);
        var project = await CreateProjectAsync(client, household.Id.Value, "Renovation");
        var projectId = project.ProjectId;

        var first = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 3000, new DateOnly(2026, 6, 5), "Counters");
        var second = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 500, new DateOnly(2026, 6, 6), "Paint");
        var unlinked = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 999, new DateOnly(2026, 6, 7), "Unrelated");

        await AssignTransactionToProjectAsync(client, household.Id.Value, first.TransactionId, projectId);
        await AssignTransactionToProjectAsync(client, household.Id.Value, second.TransactionId, projectId);

        using var scope = fixture.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var summary = await bus.InvokeAsync<GetProjectSpendSummaryResult>(
            new GetProjectSpendSummaryQuery(household.Id.Value, [projectId]));

        var item = Assert.Single(summary.Summaries);
        Assert.Equal(projectId, item.ProjectId);
        Assert.Equal(3500m, item.LinkedTotal.Amount);
        Assert.Equal("SEK", item.LinkedTotal.Currency);
        Assert.Equal(2, item.TransactionCount);
        Assert.NotEqual(unlinked.TransactionId, first.TransactionId);
    }

    [Fact]
    public async Task Transactions_RejectUnknownProjectLink()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "unknown-project");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var transaction = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 250, new DateOnly(2026, 6, 5), "Tiles");

        var assigned = await AssignTransactionToProjectAsync(client, household.Id.Value, transaction.TransactionId, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, assigned.StatusCode);
    }

    [Fact]
    public async Task ProjectDeletedV1_NullsLinkedTransactions()
    {
        var ownerId = Guid.NewGuid();
        var household = await CreateHouseholdAsync(ownerId, "Acme", "project-deleted");
        using var client = fixture.CreateAuthenticatedClient(ownerId, "owner@example.com", "Owner");
        await CreateSettingsAsync(client, household.Id.Value);
        var account = await CreateAccountAsync(client, household.Id.Value, "Checking", "Spending", 1000);
        var projectId = Guid.NewGuid();
        var transaction = await RecordTransactionAsync(client, household.Id.Value, account.AccountId, 250, new DateOnly(2026, 6, 5), "Tiles");
        await AssignTransactionToProjectAsync(client, household.Id.Value, transaction.TransactionId, projectId);

        await fixture.ApplicationHost.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(10))
            .InvokeMessageAndWaitAsync(new ProjectDeletedV1(household.Id.Value, projectId, Guid.NewGuid()));

        var linkedProjectId = await fixture.QueryDbAsync<EconomyDbContext, Guid?>((db, ct) =>
            db.Transactions
                .Where(t => t.Id == new Domain.TransactionId(transaction.TransactionId))
                .Select(t => t.ProjectId)
                .SingleAsync(ct));

        Assert.Null(linkedProjectId);
    }

    private static async Task<HttpResponseMessage> AssignTransactionToProjectAsync(
        HttpClient client,
        Guid householdId,
        Guid transactionId,
        Guid? projectId) =>
        await client.PostAsJsonAsync(
            $"/v1/economy/transactions/{transactionId}/project",
            new AssignTransactionToProjectRequest(householdId, projectId));

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
            new CreateAccountRequest(householdId, name, type, new MoneyDto(openingBalance, "SEK")));
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
                new MoneyDto(amount, "SEK"),
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

    private static async Task<SubscriptionResponse> CreateSubscriptionAsync(
        HttpClient client,
        Guid householdId,
        Guid accountId,
        string name,
        decimal expectedAmount,
        int chargeDay,
        DateOnly startsOn)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/economy/subscriptions",
            new CreateSubscriptionRequest(
                householdId,
                name,
                "Monthly",
                1,
                chargeDay,
                new MoneyDto(expectedAmount, "SEK"),
                "Active",
                null,
                accountId,
                startsOn));
        response.EnsureSuccessStatusCode();
        var subscription = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(subscription);
        return subscription;
    }

    private static Task<TransactionResponse> RecordTransactionAsync(
        HttpClient client,
        Guid householdId,
        Guid accountId,
        decimal amount,
        DateOnly occurredOn,
        string note) =>
        RecordTransactionAsync(client, householdId, accountId, null, amount, occurredOn, note, "Expense");

    private static async Task<TransactionResponse> RecordTransactionAsync(
        HttpClient client,
        Guid householdId,
        Guid accountId,
        Guid? categoryId,
        decimal amount,
        DateOnly occurredOn,
        string note,
        string kind)
        => await RecordTransactionAsync(client, householdId, accountId, categoryId, amount, occurredOn, note, kind, payerId: null);

    private static async Task<TransactionResponse> RecordTransactionAsync(
        HttpClient client,
        Guid householdId,
        Guid accountId,
        Guid? categoryId,
        decimal amount,
        DateOnly occurredOn,
        string note,
        string kind,
        Guid? payerId)
    {
        var response = await client.PostAsJsonAsync(
            "/v1/economy/transactions",
            new RecordTransactionRequest(
                householdId,
                accountId,
                categoryId,
                new MoneyDto(amount, "SEK"),
                occurredOn,
                note,
                kind,
                payerId));
        response.EnsureSuccessStatusCode();
        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(transaction);
        return transaction;
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

    private static async Task AttachReceiptAsync(HttpClient client, Guid householdId, Guid transactionId)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(householdId.ToString()), "householdId");

        var file = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "receipt.png");

        var response = await client.PostAsync($"/v1/economy/transactions/{transactionId}/receipt", content);
        response.EnsureSuccessStatusCode();
    }

    private static async Task LinkSubscriptionAsync(HttpClient client, Guid householdId, Guid subscriptionId, Guid transactionId)
    {
        var response = await client.PostAsJsonAsync(
            $"/v1/economy/subscriptions/{subscriptionId}/link",
            new LinkTransactionRequest(householdId, transactionId));
        response.EnsureSuccessStatusCode();
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

    private async Task AddHouseholdMemberAsync(HouseholdId householdId, Guid userId)
    {
        await fixture.ExecuteDbAsync<HouseholdsDbContext>(async (db, ct) =>
        {
            var clock = fixture.Services.GetRequiredService<IClock>();
            var household = await db.Households
                .Include(household => household.Memberships)
                .SingleAsync(household => household.Id == householdId, ct);
            var result = household.AddMember(userId, HouseholdRole.Member, clock);
            Assert.False(result.IsError);
            await db.SaveChangesAsync(ct);
        });
    }
}
