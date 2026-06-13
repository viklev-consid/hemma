using FluentValidation;
using Hemma.Modules.Property.Contracts.Authorization;
using Hemma.Modules.Property.Features.AddAttachment;
using Hemma.Modules.Property.Features.AddLink;
using Hemma.Modules.Property.Features.AddTask;
using Hemma.Modules.Property.Features.ArchiveArea;
using Hemma.Modules.Property.Features.ArchiveTag;
using Hemma.Modules.Property.Features.AssignTags;
using Hemma.Modules.Property.Features.ChangeIssueStatus;
using Hemma.Modules.Property.Features.ChangeProjectStatus;
using Hemma.Modules.Property.Features.ClearOccurrenceSnooze;
using Hemma.Modules.Property.Features.CompleteOccurrence;
using Hemma.Modules.Property.Features.CreateArea;
using Hemma.Modules.Property.Features.CreateHistoryEntry;
using Hemma.Modules.Property.Features.CreateMaintenancePlan;
using Hemma.Modules.Property.Features.CreateProject;
using Hemma.Modules.Property.Features.CreateTag;
using Hemma.Modules.Property.Features.DeactivatePlan;
using Hemma.Modules.Property.Features.DeleteHistoryEntry;
using Hemma.Modules.Property.Features.DeleteIssue;
using Hemma.Modules.Property.Features.DeletePlan;
using Hemma.Modules.Property.Features.DeleteProject;
using Hemma.Modules.Property.Features.DeleteTask;
using Hemma.Modules.Property.Features.GetAttachmentContent;
using Hemma.Modules.Property.Features.GetHistoryPhoto;
using Hemma.Modules.Property.Features.GetIssue;
using Hemma.Modules.Property.Features.GetMaintenancePlan;
using Hemma.Modules.Property.Features.GetProject;
using Hemma.Modules.Property.Features.GetProjectBudget;
using Hemma.Modules.Property.Features.GetProjectTasks;
using Hemma.Modules.Property.Features.GetPropertyActivitySummary;
using Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;
using Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;
using Hemma.Modules.Property.Features.ListAreas;
using Hemma.Modules.Property.Features.ListHistory;
using Hemma.Modules.Property.Features.ListIssues;
using Hemma.Modules.Property.Features.ListMaintenancePlans;
using Hemma.Modules.Property.Features.ListProjects;
using Hemma.Modules.Property.Features.ListPropertyActivity;
using Hemma.Modules.Property.Features.ListTags;
using Hemma.Modules.Property.Features.ListTimeline;
using Hemma.Modules.Property.Features.ListUpcomingOccurrences;
using Hemma.Modules.Property.Features.PromoteIssueToProject;
using Hemma.Modules.Property.Features.PromoteOccurrenceToProject;
using Hemma.Modules.Property.Features.RemoveAttachment;
using Hemma.Modules.Property.Features.RemoveLink;
using Hemma.Modules.Property.Features.ReorderAreas;
using Hemma.Modules.Property.Features.ReorderTasks;
using Hemma.Modules.Property.Features.ReportIssue;
using Hemma.Modules.Property.Features.Shared;
using Hemma.Modules.Property.Features.SkipOccurrence;
using Hemma.Modules.Property.Features.SnoozeOccurrence;
using Hemma.Modules.Property.Features.UnlinkIssue;
using Hemma.Modules.Property.Features.UpdateArea;
using Hemma.Modules.Property.Features.UpdateHistoryEntry;
using Hemma.Modules.Property.Features.UpdateIssue;
using Hemma.Modules.Property.Features.UpdateMaintenancePlan;
using Hemma.Modules.Property.Features.UpdateProject;
using Hemma.Modules.Property.Features.UpdateTag;
using Hemma.Modules.Property.Features.UpdateTask;
using Hemma.Modules.Property.Gdpr;
using Hemma.Modules.Property.Integration;
using Hemma.Modules.Property.Integration.Subscribers;
using Hemma.Modules.Property.Jobs;
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
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
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
        services.AddScoped<ActivityOperations>();
        services.AddScoped<AreasTagsOperations>();
        services.AddScoped<IssuesOperations>();
        services.AddScoped<LogbookOperations>();
        services.AddScoped<MaintenanceOperations>();
        services.AddScoped<ProjectsOperations>();
        services.AddScoped<PropertyNotificationDispatcher>();

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
        opts.Discovery.IncludeType<AddAttachmentHandler>();
        opts.Discovery.IncludeType<AddLinkHandler>();
        opts.Discovery.IncludeType<AddTaskHandler>();
        opts.Discovery.IncludeType<ArchiveAreaHandler>();
        opts.Discovery.IncludeType<ArchiveTagHandler>();
        opts.Discovery.IncludeType<AssignTagsHandler>();
        opts.Discovery.IncludeType<ChangeIssueStatusHandler>();
        opts.Discovery.IncludeType<ChangeProjectStatusHandler>();
        opts.Discovery.IncludeType<ClearOccurrenceSnoozeHandler>();
        opts.Discovery.IncludeType<CompleteOccurrenceHandler>();
        opts.Discovery.IncludeType<CreateAreaHandler>();
        opts.Discovery.IncludeType<CreateHistoryEntryHandler>();
        opts.Discovery.IncludeType<CreateMaintenancePlanHandler>();
        opts.Discovery.IncludeType<CreateProjectHandler>();
        opts.Discovery.IncludeType<CreateTagHandler>();
        opts.Discovery.IncludeType<DeactivatePlanHandler>();
        opts.Discovery.IncludeType<DeleteHistoryEntryHandler>();
        opts.Discovery.IncludeType<DeleteIssueHandler>();
        opts.Discovery.IncludeType<DeletePlanHandler>();
        opts.Discovery.IncludeType<DeleteProjectHandler>();
        opts.Discovery.IncludeType<DeleteTaskHandler>();
        opts.Discovery.IncludeType<GetAttachmentContentHandler>();
        opts.Discovery.IncludeType<GetHistoryPhotoHandler>();
        opts.Discovery.IncludeType<GetIssueHandler>();
        opts.Discovery.IncludeType<GetMaintenancePlanHandler>();
        opts.Discovery.IncludeType<GetPropertyActivitySummaryHandler>();
        opts.Discovery.IncludeType<GetProjectHandler>();
        opts.Discovery.IncludeType<GetProjectBudgetHandler>();
        opts.Discovery.IncludeType<GetProjectTasksHandler>();
        opts.Discovery.IncludeType<LinkIssueToMaintenanceOccurrenceHandler>();
        opts.Discovery.IncludeType<LinkIssueToMaintenancePlanHandler>();
        opts.Discovery.IncludeType<ListAreasHandler>();
        opts.Discovery.IncludeType<ListHistoryHandler>();
        opts.Discovery.IncludeType<ListIssuesHandler>();
        opts.Discovery.IncludeType<ListMaintenancePlansHandler>();
        opts.Discovery.IncludeType<ListPropertyActivityHandler>();
        opts.Discovery.IncludeType<ListProjectsHandler>();
        opts.Discovery.IncludeType<ListTagsHandler>();
        opts.Discovery.IncludeType<ListTimelineHandler>();
        opts.Discovery.IncludeType<ListUpcomingOccurrencesHandler>();
        opts.Discovery.IncludeType<PromoteIssueToProjectHandler>();
        opts.Discovery.IncludeType<PromoteOccurrenceToProjectHandler>();
        opts.Discovery.IncludeType<RemoveAttachmentHandler>();
        opts.Discovery.IncludeType<RemoveLinkHandler>();
        opts.Discovery.IncludeType<ReorderAreasHandler>();
        opts.Discovery.IncludeType<ReorderTasksHandler>();
        opts.Discovery.IncludeType<ReportIssueHandler>();
        opts.Discovery.IncludeType<SkipOccurrenceHandler>();
        opts.Discovery.IncludeType<SnoozeOccurrenceHandler>();
        opts.Discovery.IncludeType<UnlinkIssueHandler>();
        opts.Discovery.IncludeType<UpdateAreaHandler>();
        opts.Discovery.IncludeType<UpdateHistoryEntryHandler>();
        opts.Discovery.IncludeType<UpdateIssueHandler>();
        opts.Discovery.IncludeType<UpdateMaintenancePlanHandler>();
        opts.Discovery.IncludeType<UpdateProjectHandler>();
        opts.Discovery.IncludeType<UpdateTagHandler>();
        opts.Discovery.IncludeType<UpdateTaskHandler>();
        opts.Discovery.IncludeType<MaterializeMaintenanceOccurrencesHandler>();
        return opts;
    }

    public static TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> AddPropertyJobs(
        this TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> opts)
    {
        _ = typeof(MaterializeMaintenanceOccurrencesJob);
        return opts;
    }

    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder app)
    {
        AddAttachmentEndpoint.Map(app);
        AddLinkEndpoint.Map(app);
        AddTaskEndpoint.Map(app);
        ArchiveAreaEndpoint.Map(app);
        ArchiveTagEndpoint.Map(app);
        AssignTagsEndpoint.Map(app);
        ChangeIssueStatusEndpoint.Map(app);
        ChangeProjectStatusEndpoint.Map(app);
        ClearOccurrenceSnoozeEndpoint.Map(app);
        CompleteOccurrenceEndpoint.Map(app);
        CreateAreaEndpoint.Map(app);
        CreateHistoryEntryEndpoint.Map(app);
        CreateMaintenancePlanEndpoint.Map(app);
        CreateProjectEndpoint.Map(app);
        CreateTagEndpoint.Map(app);
        DeactivatePlanEndpoint.Map(app);
        DeleteHistoryEntryEndpoint.Map(app);
        DeleteIssueEndpoint.Map(app);
        DeletePlanEndpoint.Map(app);
        DeleteProjectEndpoint.Map(app);
        DeleteTaskEndpoint.Map(app);
        GetAttachmentContentEndpoint.Map(app);
        GetHistoryPhotoEndpoint.Map(app);
        GetIssueEndpoint.Map(app);
        GetMaintenancePlanEndpoint.Map(app);
        GetPropertyActivitySummaryEndpoint.Map(app);
        GetProjectEndpoint.Map(app);
        GetProjectBudgetEndpoint.Map(app);
        GetProjectTasksEndpoint.Map(app);
        LinkIssueToMaintenanceOccurrenceEndpoint.Map(app);
        LinkIssueToMaintenancePlanEndpoint.Map(app);
        ListAreasEndpoint.Map(app);
        ListHistoryEndpoint.Map(app);
        ListIssuesEndpoint.Map(app);
        ListMaintenancePlansEndpoint.Map(app);
        ListPropertyActivityEndpoint.Map(app);
        ListProjectsEndpoint.Map(app);
        ListTagsEndpoint.Map(app);
        ListTimelineEndpoint.Map(app);
        ListUpcomingOccurrencesEndpoint.Map(app);
        PromoteIssueToProjectEndpoint.Map(app);
        PromoteOccurrenceToProjectEndpoint.Map(app);
        RemoveAttachmentEndpoint.Map(app);
        RemoveLinkEndpoint.Map(app);
        ReorderAreasEndpoint.Map(app);
        ReorderTasksEndpoint.Map(app);
        ReportIssueEndpoint.Map(app);
        SkipOccurrenceEndpoint.Map(app);
        SnoozeOccurrenceEndpoint.Map(app);
        UnlinkIssueEndpoint.Map(app);
        UpdateAreaEndpoint.Map(app);
        UpdateHistoryEntryEndpoint.Map(app);
        UpdateIssueEndpoint.Map(app);
        UpdateMaintenancePlanEndpoint.Map(app);
        UpdateProjectEndpoint.Map(app);
        UpdateTagEndpoint.Map(app);
        UpdateTaskEndpoint.Map(app);
        return app;
    }
}
