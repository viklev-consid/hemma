using FluentValidation;
using Hemma.Modules.Property.Features.Projects;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Gdpr;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Integration.Subscribers;
using Hemma.Modules.Property.Persistence;
using Hemma.Modules.Property.Seeding;
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

namespace Hemma.Modules.Property;

public static class PropertyModule
{
    public static IServiceCollection AddPropertyModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddPermissions(PropertyPermissions.All);

        services.AddDbContext<PropertyDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("db"),
                b => b.MigrationsHistoryTable("__ef_migrations_history", "property"));
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddValidatorsFromAssemblyContaining<PropertyDbContext>(
            ServiceLifetime.Scoped, includeInternalTypes: true);

        services.AddScoped<IPersonalDataExporter, PropertyPersonalDataExporter>();
        services.AddScoped<PropertyPersonalDataEraser>();
        services.AddScoped<IPersonalDataEraser>(sp => sp.GetRequiredService<PropertyPersonalDataEraser>());
        services.AddScoped<PropertyAuditPublisher>();

        services.AddHealthChecks()
            .AddDbContextCheck<PropertyDbContext>("property-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddSource(PropertyTelemetry.SourceName))
            .WithMetrics(m => m.AddMeter(PropertyTelemetry.MeterName));

        if (environment.IsDevelopment())
        {
            services.AddScoped<IModuleSeeder, PropertyDevSeeder>();
        }

        return services;
    }

    public static WolverineOptions AddPropertyHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeType<OnUserErasureRequestedHandler>();
        opts.Discovery.IncludeType<OnHouseholdDeletedHandler>();
        opts.Discovery.IncludeType<ProjectHandler>();
        return opts;
    }

    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder app)
    {
        ProjectEndpoint.Map(app);
        return app;
    }
}
