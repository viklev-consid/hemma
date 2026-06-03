using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Hemma.Modules.Notifications.Contracts.Authorization;
using Hemma.Modules.Notifications.Features.ArchiveNotification;
using Hemma.Modules.Notifications.Features.CreateNotification;
using Hemma.Modules.Notifications.Features.GetMyNotificationPreferences;
using Hemma.Modules.Notifications.Features.GetUnreadNotificationCount;
using Hemma.Modules.Notifications.Features.ListMyNotifications;
using Hemma.Modules.Notifications.Features.MarkAllNotificationsAsRead;
using Hemma.Modules.Notifications.Features.MarkNotificationAsRead;
using Hemma.Modules.Notifications.Features.StreamMyNotifications;
using Hemma.Modules.Notifications.Features.UpdateMyNotificationPreferences;
using Hemma.Modules.Notifications.Gdpr;
using Hemma.Modules.Notifications.Integration.Subscribers;
using Hemma.Modules.Notifications.Jobs;
using Hemma.Modules.Notifications.Persistence;
using Hemma.Modules.Notifications.Policies;
using Hemma.Modules.Notifications.Streaming;
using Hemma.Modules.Notifications.Templates;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Notifications;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Infrastructure.Time;
using Hemma.Shared.Kernel.Interfaces;
using OpenTelemetry;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using Wolverine;

namespace Hemma.Modules.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection("Modules:Notifications"))
            .ValidateDataAnnotations()
            .Validate(options => options.Stream.MaxActiveStreamsPerUser is >= 1 and <= 10,
                "Notification stream max active streams per user must be between 1 and 10.")
            .Validate(options => options.Stream.ChannelCapacity is >= 1 and <= 1000,
                "Notification stream channel capacity must be between 1 and 1000.")
            .ValidateOnStart();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection("Modules:Notifications:Smtp"))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddPermissions(NotificationsPermissions.All);

        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<INotificationStreamPublisher, InMemoryNotificationStreamPublisher>();
        services.AddScoped<NotificationRetentionPolicy>();

        services.AddDbContext<NotificationsDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("db"),
                b => b.MigrationsHistoryTable("__ef_migrations_history", "notifications"));
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<IPersonalDataExporter, NotificationsPersonalDataExporter>();
        services.AddScoped<NotificationsPersonalDataEraser>();
        services.AddScoped<IPersonalDataEraser>(sp => sp.GetRequiredService<NotificationsPersonalDataEraser>());
        services.AddScoped<NotificationSendGuard>();
        services.AddSingleton<IEmailTemplateRenderer, FileEmailTemplateRenderer>();

        services.AddHealthChecks()
            .AddDbContextCheck<NotificationsDbContext>("notifications-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddSource(NotificationsTelemetry.SourceName))
            .WithMetrics(m => m.AddMeter(NotificationsTelemetry.MeterName));

        return services;
    }

    public static WolverineOptions AddNotificationsHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeType<OnUserEmailConfirmedHandler>();
        opts.Discovery.IncludeType<OnPasswordResetRequestedHandler>();
        opts.Discovery.IncludeType<OnPasswordResetHandler>();
        opts.Discovery.IncludeType<OnPasswordChangedHandler>();
        opts.Discovery.IncludeType<OnEmailConfirmationRequestedHandler>();
        opts.Discovery.IncludeType<OnEmailChangeRequestedHandler>();
        opts.Discovery.IncludeType<OnEmailChangedHandler>();
        opts.Discovery.IncludeType<OnUserInvitationCreatedHandler>();
        opts.Discovery.IncludeType<OnOrganizationInvitationCreatedHandler>();

        opts.Discovery.IncludeType<OnTwoFactorEnabledHandler>();
        opts.Discovery.IncludeType<OnTwoFactorDisabledHandler>();
        opts.Discovery.IncludeType<OnRecoveryCodesRegeneratedHandler>();
        opts.Discovery.IncludeType<OnUserErasureRequestedHandler>();
        opts.Discovery.IncludeType<CreateNotificationHandler>();
        opts.Discovery.IncludeType<ListMyNotificationsHandler>();
        opts.Discovery.IncludeType<GetUnreadNotificationCountHandler>();
        opts.Discovery.IncludeType<MarkNotificationAsReadHandler>();
        opts.Discovery.IncludeType<MarkAllNotificationsAsReadHandler>();
        opts.Discovery.IncludeType<ArchiveNotificationHandler>();
        opts.Discovery.IncludeType<GetMyNotificationPreferencesHandler>();
        opts.Discovery.IncludeType<UpdateMyNotificationPreferencesHandler>();
        opts.Discovery.IncludeType<StreamMyNotificationsHandler>();
        opts.Discovery.IncludeType<PruneNotificationsHandler>();
        return opts;
    }

    public static TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> AddNotificationsJobs(
        this TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> opts)
    {
        _ = typeof(PruneNotificationsJob);
        return opts;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        ListMyNotificationsEndpoint.Map(app);
        GetUnreadNotificationCountEndpoint.Map(app);
        MarkNotificationAsReadEndpoint.Map(app);
        MarkAllNotificationsAsReadEndpoint.Map(app);
        ArchiveNotificationEndpoint.Map(app);
        StreamMyNotificationsEndpoint.Map(app);
        GetMyNotificationPreferencesEndpoint.Map(app);
        UpdateMyNotificationPreferencesEndpoint.Map(app);
        return app;
    }
}
