using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Hemma.Modules.Organizations.Authorization;
using Hemma.Modules.Organizations.Contracts.Authorization;
using Hemma.Modules.Organizations.Features.AcceptOrganizationInvitation;
using Hemma.Modules.Organizations.Features.ChangeOrganizationMemberRole;
using Hemma.Modules.Organizations.Features.CreateOrganization;
using Hemma.Modules.Organizations.Features.CreateOrganizationInvitation;
using Hemma.Modules.Organizations.Features.DeleteOrganization;
using Hemma.Modules.Organizations.Features.EnsureUserCanBeErasedFromOrganizations;
using Hemma.Modules.Organizations.Features.GetOrganization;
using Hemma.Modules.Organizations.Features.GetOrganizationAudit;
using Hemma.Modules.Organizations.Features.ListMyOrganizations;
using Hemma.Modules.Organizations.Features.ListOrganizationInvitations;
using Hemma.Modules.Organizations.Features.ListOrganizationMembers;
using Hemma.Modules.Organizations.Features.RemoveOrganizationMember;
using Hemma.Modules.Organizations.Features.RevokeOrganizationInvitation;
using Hemma.Modules.Organizations.Features.UpdateOrganization;
using Hemma.Modules.Organizations.Features.ValidateOrganizationInvitationForRegistration;
using Hemma.Modules.Organizations.Gdpr;
using Hemma.Modules.Organizations.Integration.Subscribers;
using Hemma.Modules.Organizations.Persistence;
using Hemma.Modules.Organizations.Seeding;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Infrastructure.Seeding;
using Hemma.Shared.Infrastructure.Time;
using Hemma.Shared.Kernel.Interfaces;
using OpenTelemetry;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace Hemma.Modules.Organizations;

public static class OrganizationsModule
{
    public static IServiceCollection AddOrganizationsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<OrganizationsOptions>()
            .Bind(configuration.GetSection("Modules:Organizations"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<IClock, SystemClock>();
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddPermissions(OrganizationsPermissions.All);
        services.AddScoped<IOrganizationRefResolver, OrganizationRefResolver>();
        services.AddScoped<IScopedAuthorizationService<OrganizationScope>, OrganizationScopedAuthorizationService>();

        services.AddDbContextWithWolverineIntegration<OrganizationsDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("db"),
                b => b.MigrationsHistoryTable("__ef_migrations_history", "organizations"));
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddValidatorsFromAssemblyContaining<OrganizationsDbContext>(
            ServiceLifetime.Scoped, includeInternalTypes: true);

        services.AddScoped<IPersonalDataExporter, OrganizationsPersonalDataExporter>();
        services.AddScoped<OrganizationsPersonalDataEraser>();
        services.AddScoped<IPersonalDataEraser>(sp => sp.GetRequiredService<OrganizationsPersonalDataEraser>());

        services.AddHealthChecks()
            .AddDbContextCheck<OrganizationsDbContext>("organizations-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddSource(OrganizationsTelemetry.SourceName))
            .WithMetrics(m => m.AddMeter(OrganizationsTelemetry.MeterName));

        if (environment.IsDevelopment())
        {
            services.AddScoped<IModuleSeeder, OrganizationsDevSeeder>();
        }

        return services;
    }

    public static WolverineOptions AddOrganizationsHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeType<CreateOrganizationHandler>();
        opts.Discovery.IncludeType<ListMyOrganizationsHandler>();
        opts.Discovery.IncludeType<GetOrganizationHandler>();
        opts.Discovery.IncludeType<UpdateOrganizationHandler>();
        opts.Discovery.IncludeType<DeleteOrganizationHandler>();
        opts.Discovery.IncludeType<ListOrganizationMembersHandler>();
        opts.Discovery.IncludeType<ChangeOrganizationMemberRoleHandler>();
        opts.Discovery.IncludeType<RemoveOrganizationMemberHandler>();
        opts.Discovery.IncludeType<CreateOrganizationInvitationHandler>();
        opts.Discovery.IncludeType<AcceptOrganizationInvitationHandler>();
        opts.Discovery.IncludeType<ListOrganizationInvitationsHandler>();
        opts.Discovery.IncludeType<RevokeOrganizationInvitationHandler>();
        opts.Discovery.IncludeType<EnsureUserCanBeErasedFromOrganizationsHandler>();
        opts.Discovery.IncludeType<ValidateOrganizationInvitationForRegistrationHandler>();
        opts.Discovery.IncludeType<OnUserErasureRequestedHandler>();
        return opts;
    }

    public static IEndpointRouteBuilder MapOrganizationsEndpoints(this IEndpointRouteBuilder app)
    {
        CreateOrganizationEndpoint.Map(app);
        ListMyOrganizationsEndpoint.Map(app);
        GetOrganizationEndpoint.Map(app);
        UpdateOrganizationEndpoint.Map(app);
        DeleteOrganizationEndpoint.Map(app);
        ListOrganizationMembersEndpoint.Map(app);
        ChangeOrganizationMemberRoleEndpoint.Map(app);
        RemoveOrganizationMemberEndpoint.Map(app);
        CreateOrganizationInvitationEndpoint.Map(app);
        AcceptOrganizationInvitationEndpoint.Map(app);
        ListOrganizationInvitationsEndpoint.Map(app);
        RevokeOrganizationInvitationEndpoint.Map(app);
        GetOrganizationAuditEndpoint.Map(app);
        return app;
    }
}
