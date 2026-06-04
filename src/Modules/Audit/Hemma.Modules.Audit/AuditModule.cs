using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hemma.Modules.Audit.Authorization;
using Hemma.Modules.Audit.Contracts.Authorization;
using Hemma.Modules.Audit.Features.GetAuditTrail;
using Hemma.Modules.Audit.Features.ListHouseholdAuditEntries;
using Hemma.Modules.Audit.Gdpr;
using Hemma.Modules.Audit.Integration.Subscribers;
using Hemma.Modules.Audit.Persistence;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using OpenTelemetry;
using Wolverine;

namespace Hemma.Modules.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();
        services.AddPermissions(AuditPermissions.All);

        services.AddDbContext<AuditDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("db"),
                b => b.MigrationsHistoryTable("__ef_migrations_history", "audit"));
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<IPersonalDataExporter, AuditPersonalDataExporter>();
        services.AddScoped<AuditPersonalDataEraser>();
        services.AddScoped<IPersonalDataEraser>(sp => sp.GetRequiredService<AuditPersonalDataEraser>());
        services.AddScoped<HouseholdAuditWriter>();

        services.AddSingleton<IResourcePolicy<AuditTrailResource>, AuditTrailPolicy>();

        services.AddHealthChecks()
            .AddDbContextCheck<AuditDbContext>("audit-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddSource(AuditTelemetry.SourceName))
            .WithMetrics(m => m.AddMeter(AuditTelemetry.MeterName));

        return services;
    }

    public static WolverineOptions AddAuditHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeType<GetAuditTrailHandler>();
        opts.Discovery.IncludeType<ListHouseholdAuditEntriesHandler>();
        opts.Discovery.IncludeType<OnUserRegisteredHandler>();
        opts.Discovery.IncludeType<OnUserEmailChangedHandler>();
        opts.Discovery.IncludeType<OnUserLoggedInHandler>();
        opts.Discovery.IncludeType<OnUserLoggedOutHandler>();
        opts.Discovery.IncludeType<OnUserLoggedOutAllDevicesHandler>();
        opts.Discovery.IncludeType<OnPasswordResetHandler>();
        opts.Discovery.IncludeType<OnPasswordChangedHandler>();
        opts.Discovery.IncludeType<OnEmailChangedHandler>();
        opts.Discovery.IncludeType<OnUserRoleChangedHandler>();
        opts.Discovery.IncludeType<OnUserProfileUpdatedHandler>();
        opts.Discovery.IncludeType<OnUserAvatarUpdatedHandler>();
        opts.Discovery.IncludeType<OnUserAvatarRemovedHandler>();

        opts.Discovery.IncludeType<OnUserOnboardingCompletedHandler>();
        opts.Discovery.IncludeType<OnUserErasureRequestedHandler>();
        opts.Discovery.IncludeType<OnRefreshTokenReuseDetectedHandler>();
        opts.Discovery.IncludeType<OnTwoFactorEnabledHandler>();
        opts.Discovery.IncludeType<OnTwoFactorDisabledHandler>();
        opts.Discovery.IncludeType<OnRecoveryCodesRegeneratedHandler>();
        opts.Discovery.IncludeType<OnHouseholdCreatedHandler>();
        opts.Discovery.IncludeType<OnHouseholdDeletedHandler>();
        opts.Discovery.IncludeType<OnHouseholdInvitationCreatedHandler>();
        opts.Discovery.IncludeType<OnHouseholdMemberAddedHandler>();
        opts.Discovery.IncludeType<OnHouseholdMemberRemovedHandler>();
        opts.Discovery.IncludeType<OnHouseholdMemberRoleChangedHandler>();
        return opts;
    }

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        GetAuditTrailEndpoint.Map(app);
        return app;
    }
}
