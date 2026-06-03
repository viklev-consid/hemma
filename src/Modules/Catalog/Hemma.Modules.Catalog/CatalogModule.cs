using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hemma.Modules.Catalog.Contracts.Authorization;
using Hemma.Modules.Catalog.Features.CreateProduct;
using Hemma.Modules.Catalog.Features.GetProductById;
using Hemma.Modules.Catalog.Features.ListProducts;
using Hemma.Modules.Catalog.Gdpr;
using Hemma.Modules.Catalog.Integration.Subscribers;
using Hemma.Modules.Catalog.Persistence;
using Hemma.Modules.Catalog.Seeding;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Infrastructure.Seeding;
using Hemma.Shared.Kernel.Interfaces;
using OpenTelemetry;
using Wolverine;

namespace Hemma.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<CatalogOptions>()
            .Bind(configuration.GetSection("Modules:Catalog"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();
        services.AddPermissions(CatalogPermissions.All);

        services.AddDbContext<CatalogDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("db"),
                b => b.MigrationsHistoryTable("__ef_migrations_history", "catalog"));
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddValidatorsFromAssemblyContaining<CreateProductValidator>(ServiceLifetime.Scoped, includeInternalTypes: true);

        services.AddScoped<IPersonalDataExporter, CatalogPersonalDataExporter>();
        services.AddScoped<CatalogPersonalDataEraser>();
        services.AddScoped<IPersonalDataEraser>(sp => sp.GetRequiredService<CatalogPersonalDataEraser>());

        services.AddHealthChecks()
            .AddDbContextCheck<CatalogDbContext>("catalog-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddSource(CatalogTelemetry.SourceName))
            .WithMetrics(m => m.AddMeter(CatalogTelemetry.MeterName));

        if (environment.IsDevelopment())
        {
            services.AddScoped<IModuleSeeder, CatalogDevSeeder>();
        }

        return services;
    }

    public static WolverineOptions AddCatalogHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeType<CreateProductHandler>();
        opts.Discovery.IncludeType<GetProductByIdHandler>();
        opts.Discovery.IncludeType<ListProductsHandler>();
        opts.Discovery.IncludeType<OnUserRegisteredHandler>();
        opts.Discovery.IncludeType<OnUserErasureRequestedHandler>();
        opts.Discovery.IncludeType<OnEmailChangedHandler>();
        opts.Discovery.IncludeType<OnUserProfileUpdatedHandler>();
        return opts;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        ListProductsEndpoint.Map(app);
        CreateProductEndpoint.Map(app);
        GetProductByIdEndpoint.Map(app);
        return app;
    }
}
