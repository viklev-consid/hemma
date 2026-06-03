using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hemma.Modules.Users.Avatars;
using Hemma.Modules.Users.ConsentManagement;
using Hemma.Modules.Users.Contracts.Authorization;
using Hemma.Modules.Users.Features.AcceptLegalDocuments;
using Hemma.Modules.Users.Features.ChangePassword;
using Hemma.Modules.Users.Features.ChangeUserRole;
using Hemma.Modules.Users.Features.CompleteOnboarding;
using Hemma.Modules.Users.Features.ConfirmEmail;
using Hemma.Modules.Users.Features.ConfirmEmailChange;
using Hemma.Modules.Users.Features.CreateInvitation;
using Hemma.Modules.Users.Features.DeleteAccount;
using Hemma.Modules.Users.Features.DeleteAvatar;
using Hemma.Modules.Users.Features.ExportPersonalData;
using Hemma.Modules.Users.Features.ForgotPassword;
using Hemma.Modules.Users.Features.GetCurrentUser;
using Hemma.Modules.Users.Features.GetLegalDocument;
using Hemma.Modules.Users.Features.GetLegalCompliance;
using Hemma.Modules.Users.Features.GetOnboardingLegalRequirements;
using Hemma.Modules.Users.Features.GetUserAvatar;
using Hemma.Modules.Users.Features.GetUserById;
using Hemma.Modules.Users.Features.GetUserSummariesByIds;
using Hemma.Modules.Users.Features.ListInvitations;
using Hemma.Modules.Users.Features.ListUsers;
using Hemma.Modules.Users.Features.Login;
using Hemma.Modules.Users.Features.LoginTwoFactor;
using Hemma.Modules.Users.Features.Logout;
using Hemma.Modules.Users.Features.LogoutAll;
using Hemma.Modules.Users.Features.RefreshToken;
using Hemma.Modules.Users.Features.Register;
using Hemma.Modules.Users.Features.RequestEmailChange;
using Hemma.Modules.Users.Features.ResendEmailConfirmation;
using Hemma.Modules.Users.Features.ResetPassword;
using Hemma.Modules.Users.Features.RevokeInvitation;
using Hemma.Modules.Users.Features.TwoFactor.ConfirmTotp;
using Hemma.Modules.Users.Features.TwoFactor.DisableTwoFactor;
using Hemma.Modules.Users.Features.TwoFactor.RegenerateRecoveryCodes;
using Hemma.Modules.Users.Features.TwoFactor.SetupTotp;
using Hemma.Modules.Users.Features.UpdateAvatar;
using Hemma.Modules.Users.Features.UpdateProfile;
using Hemma.Modules.Users.Gdpr;
using Hemma.Modules.Users.Jobs;
using Hemma.Modules.Users.Legal;
using Hemma.Modules.Users.Persistence;
using Hemma.Modules.Users.Security;
using Hemma.Modules.Users.Seeding;
using Hemma.Shared.Infrastructure.Authorization;
using Hemma.Shared.Infrastructure.Identity;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Infrastructure.Seeding;
using Hemma.Shared.Infrastructure.Time;
using Hemma.Shared.Kernel.Interfaces;
using OpenTelemetry;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace Hemma.Modules.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<UsersOptions>()
            .Bind(configuration.GetSection("Modules:Users"))
            .ValidateDataAnnotations()
            .Validate(o => Enum.IsDefined(o.Registration.Mode), "Registration mode must be a valid value.")
            .Validate(o => o.Registration.InvitationTokenLifetime > TimeSpan.Zero, "Invitation token lifetime must be greater than zero.")
            .Validate(o => o.PasswordResetTokenLifetime > TimeSpan.Zero, "Password reset token lifetime must be greater than zero.")
            .Validate(o => o.EmailChangeTokenLifetime > TimeSpan.Zero, "Email change token lifetime must be greater than zero.")
            .Validate(o => o.EmailConfirmationTokenLifetime > TimeSpan.Zero, "Email confirmation token lifetime must be greater than zero.")
            .Validate(o => o.TwoFactorChallengeLifetime > TimeSpan.Zero, "Two-factor challenge lifetime must be greater than zero.")
            .Validate(o => !environment.IsProduction() || !string.IsNullOrWhiteSpace(o.DataProtectionKeyRingPath),
                "A shared data-protection key-ring path is required in production.")
            .ValidateOnStart();

        var dataProtection = services.AddDataProtection().SetApplicationName("Hemma");
        var dataProtectionKeyRingPath = configuration["Modules:Users:DataProtectionKeyRingPath"];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeyRingPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyRingPath));
        }

        services.AddOptions<AdminBootstrapOptions>()
            .Bind(configuration.GetSection("Modules:Users:AdminBootstrap"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddPermissions(UsersPermissions.All);
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContextWithWolverineIntegration<UsersDbContext>((sp, opts) =>
        {
            opts.UseNpgsql(
                configuration.GetConnectionString("db"),
                b => b.MigrationsHistoryTable("__ef_migrations_history", "users"));
            opts.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddMemoryCache();

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtGenerator, JwtGenerator>();
        services.AddScoped<IRefreshTokenIssuer, RefreshTokenIssuer>();
        services.AddScoped<IRefreshTokenRevoker, RefreshTokenRevoker>();
        services.AddScoped<ISingleUseTokenService, SingleUseTokenService>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<ITotpSecretProtector, DataProtectionTotpSecretProtector>();
        services.AddScoped<ITwoFactorRequirementEvaluator, TwoFactorRequirementEvaluator>();
        services.AddScoped<ITwoFactorChallengeIssuer, TwoFactorChallengeIssuer>();
        services.AddScoped<IAvatarImageInspector, MagickAvatarImageInspector>();
        services.AddScoped<IUserAvatarStorage, UserAvatarStorage>();
        services.AddScoped<ILegalComplianceService, LegalComplianceService>();
        services.AddScoped<LegalDocumentsSeeder>();

        if (!environment.IsEnvironment("Test"))
        {
            services.AddHostedService<LegalDocumentsBootstrapper>();
        }

        services.AddScoped<IConsentRegistry, UsersConsentRegistry>();
        services.AddScoped<IPersonalDataExporter, UsersPersonalDataExporter>();
        services.AddScoped<UsersPersonalDataEraser>();
        services.AddScoped<IPersonalDataEraser>(sp => sp.GetRequiredService<UsersPersonalDataEraser>());
        services.AddScoped<PersonalDataOrchestrator>();

        services.AddHealthChecks()
            .AddDbContextCheck<UsersDbContext>("users-db", tags: ["ready"]);

        services.AddOpenTelemetry()
            .WithTracing(t => t.AddSource(UsersTelemetry.SourceName))
            .WithMetrics(m => m.AddMeter(UsersTelemetry.MeterName));

        services.AddValidatorsFromAssemblyContaining<RegisterValidator>(ServiceLifetime.Scoped, includeInternalTypes: true);

        if (environment.IsDevelopment())
        {
            services.AddOptions<UsersDevOptions>()
                .Bind(configuration.GetSection("Modules:Users:Dev"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddScoped<IModuleSeeder, UsersDevSeeder>();
        }
        else
        {
            services.AddHostedService<AdminBootstrapper>();
        }

        return services;
    }

    public static WolverineOptions AddUsersHandlers(this WolverineOptions opts)
    {
        opts.Discovery.IncludeType<RegisterHandler>();
        opts.Discovery.IncludeType<LoginHandler>();
        opts.Discovery.IncludeType<LoginTwoFactorHandler>();
        opts.Discovery.IncludeType<GetCurrentUserHandler>();
        opts.Discovery.IncludeType<GetLegalDocumentHandler>();
        opts.Discovery.IncludeType<GetLegalComplianceHandler>();
        opts.Discovery.IncludeType<GetOnboardingLegalRequirementsHandler>();
        opts.Discovery.IncludeType<AcceptLegalDocumentsHandler>();
        opts.Discovery.IncludeType<CompleteOnboardingHandler>();
        opts.Discovery.IncludeType<UpdateProfileHandler>();
        opts.Discovery.IncludeType<UpdateAvatarHandler>();
        opts.Discovery.IncludeType<DeleteAvatarHandler>();
        opts.Discovery.IncludeType<GetUserAvatarHandler>();
        opts.Discovery.IncludeType<ExportPersonalDataHandler>();
        opts.Discovery.IncludeType<DeleteAccountHandler>();

        // Auth flow handlers — Phase 9.5
        opts.Discovery.IncludeType<ForgotPasswordHandler>();
        opts.Discovery.IncludeType<ResetPasswordHandler>();
        opts.Discovery.IncludeType<ChangePasswordHandler>();
        opts.Discovery.IncludeType<ConfirmEmailHandler>();
        opts.Discovery.IncludeType<RequestEmailChangeHandler>();
        opts.Discovery.IncludeType<ConfirmEmailChangeHandler>();
        opts.Discovery.IncludeType<ResendEmailConfirmationHandler>();
        opts.Discovery.IncludeType<RefreshTokenHandler>();
        opts.Discovery.IncludeType<LogoutHandler>();
        opts.Discovery.IncludeType<LogoutAllHandler>();
        opts.Discovery.IncludeType<SweepExpiredTokensHandler>();

        // RBAC management handlers — Phase 13
        opts.Discovery.IncludeType<ChangeUserRoleHandler>();
        opts.Discovery.IncludeType<ListUsersHandler>();
        opts.Discovery.IncludeType<GetUserByIdHandler>();
        opts.Discovery.IncludeType<GetUserSummariesByIdsHandler>();
        opts.Discovery.IncludeType<ListInvitationsHandler>();
        opts.Discovery.IncludeType<CreateInvitationHandler>();
        opts.Discovery.IncludeType<RevokeInvitationHandler>();

        // Two-factor authentication
        opts.Discovery.IncludeType<SetupTotpHandler>();
        opts.Discovery.IncludeType<ConfirmTotpHandler>();
        opts.Discovery.IncludeType<DisableTwoFactorHandler>();
        opts.Discovery.IncludeType<RegenerateRecoveryCodesHandler>();

        return opts;
    }

    public static TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> AddUsersJobs(
        this TickerOptionsBuilder<TimeTickerEntity, CronTickerEntity> opts)
    {
        // TickerQ discovers cron jobs from [TickerFunction] attributes. Keep this
        // extension as the module-owned registration point for future job options.
        _ = typeof(SweepExpiredTokensJob);
        return opts;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        RegisterEndpoint.Map(app);
        LoginEndpoint.Map(app);
        LoginTwoFactorEndpoint.Map(app);
        GetCurrentUserEndpoint.Map(app);
        GetLegalDocumentEndpoint.Map(app);
        GetLegalComplianceEndpoint.Map(app);
        GetOnboardingLegalRequirementsEndpoint.Map(app);
        AcceptLegalDocumentsEndpoint.Map(app);
        CompleteOnboardingEndpoint.Map(app);
        UpdateProfileEndpoint.Map(app);
        UpdateAvatarEndpoint.Map(app);
        DeleteAvatarEndpoint.Map(app);
        GetUserAvatarEndpoint.Map(app);
        ExportPersonalDataEndpoint.Map(app);
        DeleteAccountEndpoint.Map(app);

        // Auth flow endpoints — Phase 9.5
        ForgotPasswordEndpoint.Map(app);
        ResetPasswordEndpoint.Map(app);
        ChangePasswordEndpoint.Map(app);
        ConfirmEmailEndpoint.Map(app);
        RequestEmailChangeEndpoint.Map(app);
        ConfirmEmailChangeEndpoint.Map(app);
        ResendEmailConfirmationEndpoint.Map(app);
        RefreshTokenEndpoint.Map(app);
        LogoutEndpoint.Map(app);
        LogoutAllEndpoint.Map(app);

        // RBAC management endpoints — Phase 13
        ChangeUserRoleEndpoint.Map(app);
        ListUsersEndpoint.Map(app);
        GetUserByIdEndpoint.Map(app);
        ListInvitationsEndpoint.Map(app);
        CreateInvitationEndpoint.Map(app);
        RevokeInvitationEndpoint.Map(app);

        // Two-factor authentication
        SetupTotpEndpoint.Map(app);
        ConfirmTotpEndpoint.Map(app);
        DisableTwoFactorEndpoint.Map(app);
        RegenerateRecoveryCodesEndpoint.Map(app);

        return app;
    }
}
