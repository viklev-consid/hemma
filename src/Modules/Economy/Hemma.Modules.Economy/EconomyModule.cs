using FluentValidation;
using Hemma.Modules.Economy.Contracts.Authorization;
using Hemma.Modules.Economy.Gdpr;
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
        return opts;
    }

    public static IEndpointRouteBuilder MapEconomyEndpoints(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
