using FluentValidation;
using Hemma.Modules.Economy.Contracts.Authorization;
using Hemma.Modules.Economy.Features.AddCategory;
using Hemma.Modules.Economy.Features.AttachReceipt;
using Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;
using Hemma.Modules.Economy.Features.ConfirmEstimatedBill;
using Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;
using Hemma.Modules.Economy.Features.CreateAccount;
using Hemma.Modules.Economy.Features.CreateBudget;
using Hemma.Modules.Economy.Features.CreateEconomySettings;
using Hemma.Modules.Economy.Features.CreateRecurringBill;
using Hemma.Modules.Economy.Features.CreateTransfer;
using Hemma.Modules.Economy.Features.GetAccountBalances;
using Hemma.Modules.Economy.Features.GetBudgetSummary;
using Hemma.Modules.Economy.Features.ListAccounts;
using Hemma.Modules.Economy.Features.ListCategories;
using Hemma.Modules.Economy.Features.ListRecurringBills;
using Hemma.Modules.Economy.Features.ListTransactions;
using Hemma.Modules.Economy.Features.PauseOccurrence;
using Hemma.Modules.Economy.Features.RecordTransaction;
using Hemma.Modules.Economy.Features.ResumeOccurrence;
using Hemma.Modules.Economy.Features.SearchTransactionNote;
using Hemma.Modules.Economy.Features.SkipOccurrence;
using Hemma.Modules.Economy.Features.UpdateCycleStartDay;
using Hemma.Modules.Economy.Features.UpsertBudgetLine;
using Hemma.Modules.Economy.Gdpr;
using Hemma.Modules.Economy.Jobs;
using Hemma.Modules.Economy.Persistence;
using Hemma.Modules.Economy.Seeding;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Infrastructure.Seeding;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace Hemma.Modules.Economy;

public static class EconomyModule
{
    public static IServiceCollection AddEconomyModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddPermissions(EconomyPermissions.All);

        services.AddDbContextWithWolverineIntegration<EconomyDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("db"),
                b => b.MigrationsHistoryTable("__ef_migrations_history", "economy"));
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddValidatorsFromAssemblyContaining<EconomyDbContext>(
            ServiceLifetime.Scoped, includeInternalTypes: true);

        services.AddScoped<IPersonalDataExporter, EconomyPersonalDataExporter>();
        services.AddScoped<EconomyPersonalDataEraser>();
        services.AddScoped<IPersonalDataEraser>(sp => sp.GetRequiredService<EconomyPersonalDataEraser>());

        services.AddHealthChecks()
            .AddDbContextCheck<EconomyDbContext>("economy-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddSource(EconomyTelemetry.SourceName))
            .WithMetrics(m => m.AddMeter(EconomyTelemetry.MeterName));

        if (environment.IsDevelopment())
        {
            services.AddScoped<IModuleSeeder, EconomyDevSeeder>();
        }

        return services;
    }

    public static WolverineOptions AddEconomyHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeType<CreateEconomySettingsHandler>();
        opts.Discovery.IncludeType<UpdateCycleStartDayHandler>();
        opts.Discovery.IncludeType<CreateAccountHandler>();
        opts.Discovery.IncludeType<ListAccountsHandler>();
        opts.Discovery.IncludeType<AddCategoryHandler>();
        opts.Discovery.IncludeType<ListCategoriesHandler>();
        opts.Discovery.IncludeType<CreateBudgetHandler>();
        opts.Discovery.IncludeType<UpsertBudgetLineHandler>();
        opts.Discovery.IncludeType<CopyBudgetFromPreviousPeriodHandler>();
        opts.Discovery.IncludeType<RecordTransactionHandler>();
        opts.Discovery.IncludeType<AttachReceiptHandler>();
        opts.Discovery.IncludeType<ListTransactionsHandler>();
        opts.Discovery.IncludeType<SearchTransactionNoteHandler>();
        opts.Discovery.IncludeType<CreateTransferHandler>();
        opts.Discovery.IncludeType<GetAccountBalancesHandler>();
        opts.Discovery.IncludeType<GetBudgetSummaryHandler>();
        opts.Discovery.IncludeType<CreateRecurringBillHandler>();
        opts.Discovery.IncludeType<ListRecurringBillsHandler>();
        opts.Discovery.IncludeType<ConfirmEstimatedBillHandler>();
        opts.Discovery.IncludeType<ChangeRecurringBillOccurrenceHandler>();
        opts.Discovery.IncludeType<RunDueBillsHandler>();
        return opts;
    }

    public static TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> AddEconomyJobs(
        this TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> opts)
    {
        _ = typeof(RunDueBillsJob);
        return opts;
    }

    public static IEndpointRouteBuilder MapEconomyEndpoints(this IEndpointRouteBuilder app)
    {
        CreateEconomySettingsEndpoint.Map(app);
        UpdateCycleStartDayEndpoint.Map(app);
        CreateAccountEndpoint.Map(app);
        ListAccountsEndpoint.Map(app);
        AddCategoryEndpoint.Map(app);
        ListCategoriesEndpoint.Map(app);
        CreateBudgetEndpoint.Map(app);
        UpsertBudgetLineEndpoint.Map(app);
        CopyBudgetFromPreviousPeriodEndpoint.Map(app);
        RecordTransactionEndpoint.Map(app);
        AttachReceiptEndpoint.Map(app);
        ListTransactionsEndpoint.Map(app);
        SearchTransactionNoteEndpoint.Map(app);
        CreateTransferEndpoint.Map(app);
        GetAccountBalancesEndpoint.Map(app);
        GetBudgetSummaryEndpoint.Map(app);
        CreateRecurringBillEndpoint.Map(app);
        ListRecurringBillsEndpoint.Map(app);
        ConfirmEstimatedBillEndpoint.Map(app);
        SkipOccurrenceEndpoint.Map(app);
        PauseOccurrenceEndpoint.Map(app);
        ResumeOccurrenceEndpoint.Map(app);

        return app;
    }
}
