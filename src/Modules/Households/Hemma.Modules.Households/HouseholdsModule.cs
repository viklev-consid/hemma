using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Hemma.Modules.Households.Authorization;
using Hemma.Modules.Households.Contracts.Authorization;
using Hemma.Modules.Households.Features.AcceptHouseholdInvitation;
using Hemma.Modules.Households.Features.ChangeHouseholdMemberRole;
using Hemma.Modules.Households.Features.CreateHousehold;
using Hemma.Modules.Households.Features.CreateHouseholdInvitation;
using Hemma.Modules.Households.Features.DeleteHousehold;
using Hemma.Modules.Households.Features.EnsureUserCanBeErasedFromHouseholds;
using Hemma.Modules.Households.Features.GetHousehold;
using Hemma.Modules.Households.Features.GetHouseholdAudit;
using Hemma.Modules.Households.Features.ListMyHouseholds;
using Hemma.Modules.Households.Features.ListHouseholdInvitations;
using Hemma.Modules.Households.Features.ListHouseholdMembers;
using Hemma.Modules.Households.Features.RemoveHouseholdMember;
using Hemma.Modules.Households.Features.RevokeHouseholdInvitation;
using Hemma.Modules.Households.Features.UpdateHousehold;
using Hemma.Modules.Households.Features.ValidateHouseholdInvitationForRegistration;
using Hemma.Modules.Households.Gdpr;
using Hemma.Modules.Households.Integration.Subscribers;
using Hemma.Modules.Households.Persistence;
using Hemma.Modules.Households.Seeding;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Infrastructure.Seeding;
using Hemma.Shared.Infrastructure.Time;
using Hemma.Shared.Kernel.Interfaces;
using OpenTelemetry;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace Hemma.Modules.Households;

public static class HouseholdsModule
{
    public static IServiceCollection AddHouseholdsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<HouseholdsOptions>()
            .Bind(configuration.GetSection("Modules:Households"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IClock, SystemClock>();
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddPermissions(HouseholdsPermissions.All);
        services.AddScoped<IHouseholdRefResolver, HouseholdRefResolver>();
        services.AddScoped<IScopedAuthorizationService<HouseholdScope>, HouseholdScopedAuthorizationService>();

        services.AddDbContextWithWolverineIntegration<HouseholdsDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("db"),
                b => b.MigrationsHistoryTable("__ef_migrations_history", "households"));
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddValidatorsFromAssemblyContaining<HouseholdsDbContext>(
            ServiceLifetime.Scoped, includeInternalTypes: true);

        services.AddScoped<IPersonalDataExporter, HouseholdsPersonalDataExporter>();
        services.AddScoped<HouseholdsPersonalDataEraser>();
        services.AddScoped<IPersonalDataEraser>(sp => sp.GetRequiredService<HouseholdsPersonalDataEraser>());

        services.AddHealthChecks()
            .AddDbContextCheck<HouseholdsDbContext>("households-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddSource(HouseholdsTelemetry.SourceName))
            .WithMetrics(m => m.AddMeter(HouseholdsTelemetry.MeterName));

        if (environment.IsDevelopment())
        {
            services.AddScoped<IModuleSeeder, HouseholdsDevSeeder>();
        }

        return services;
    }

    public static WolverineOptions AddHouseholdsHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeType<CreateHouseholdHandler>();
        opts.Discovery.IncludeType<ListMyHouseholdsHandler>();
        opts.Discovery.IncludeType<GetHouseholdHandler>();
        opts.Discovery.IncludeType<UpdateHouseholdHandler>();
        opts.Discovery.IncludeType<DeleteHouseholdHandler>();
        opts.Discovery.IncludeType<ListHouseholdMembersHandler>();
        opts.Discovery.IncludeType<ChangeHouseholdMemberRoleHandler>();
        opts.Discovery.IncludeType<RemoveHouseholdMemberHandler>();
        opts.Discovery.IncludeType<CreateHouseholdInvitationHandler>();
        opts.Discovery.IncludeType<AcceptHouseholdInvitationHandler>();
        opts.Discovery.IncludeType<ListHouseholdInvitationsHandler>();
        opts.Discovery.IncludeType<RevokeHouseholdInvitationHandler>();
        opts.Discovery.IncludeType<EnsureUserCanBeErasedFromHouseholdsHandler>();
        opts.Discovery.IncludeType<ValidateHouseholdInvitationForRegistrationHandler>();
        opts.Discovery.IncludeType<OnUserErasureRequestedHandler>();
        return opts;
    }

    public static IEndpointRouteBuilder MapHouseholdsEndpoints(this IEndpointRouteBuilder app)
    {
        CreateHouseholdEndpoint.Map(app);
        ListMyHouseholdsEndpoint.Map(app);
        GetHouseholdEndpoint.Map(app);
        UpdateHouseholdEndpoint.Map(app);
        DeleteHouseholdEndpoint.Map(app);
        ListHouseholdMembersEndpoint.Map(app);
        ChangeHouseholdMemberRoleEndpoint.Map(app);
        RemoveHouseholdMemberEndpoint.Map(app);
        CreateHouseholdInvitationEndpoint.Map(app);
        AcceptHouseholdInvitationEndpoint.Map(app);
        ListHouseholdInvitationsEndpoint.Map(app);
        RevokeHouseholdInvitationEndpoint.Map(app);
        GetHouseholdAuditEndpoint.Map(app);
        return app;
    }
}
