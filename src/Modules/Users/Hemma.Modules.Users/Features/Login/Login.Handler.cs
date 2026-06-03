using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Errors;
using Hemma.Modules.Users.Persistence;
using Hemma.Modules.Users.Security;
using Hemma.Shared.Kernel.Interfaces;
using Wolverine;

namespace Hemma.Modules.Users.Features.Login;

public sealed class LoginHandler(
    UsersDbContext db,
    IPasswordHasher passwordHasher,
    IJwtGenerator jwtGenerator,
    IRefreshTokenIssuer refreshTokenIssuer,
    ITwoFactorRequirementEvaluator twoFactorRequirementEvaluator,
    ITwoFactorChallengeIssuer twoFactorChallengeIssuer,
    IOptions<UsersOptions> options,
    IMessageBus bus,
    IClock clock)
{
    public async Task<ErrorOr<LoginResponse>> Handle(LoginCommand cmd, CancellationToken ct)
        => await UsersTelemetry.InstrumentAsync(nameof(LoginHandler), () => HandleCoreAsync(cmd, ct));

    private async Task<ErrorOr<LoginResponse>> HandleCoreAsync(LoginCommand cmd, CancellationToken ct)
    {
        var emailResult = Email.Create(cmd.Email);
        if (emailResult.IsError)
        {
            return UsersErrors.InvalidCredentials;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailResult.Value, ct);
        if (user is null)
        {
            return UsersErrors.InvalidCredentials;
        }

        if (!passwordHasher.Verify(cmd.Password, user.PasswordHash.Value))
        {
            return UsersErrors.InvalidCredentials;
        }

        if (!user.IsEmailConfirmed)
        {
            return UsersErrors.EmailNotConfirmed;
        }

        if (await twoFactorRequirementEvaluator.IsRequiredAsync(user, ct))
        {
            var (challenge, rawChallengeToken) = twoFactorChallengeIssuer.Issue(user.Id, cmd.IpAddress);
            db.PendingTwoFactorChallenges.Add(challenge);
            await db.SaveChangesAsync(ct);

            return LoginResponse.TwoFactorRequired(new LoginChallengeResponse(rawChallengeToken, challenge.ExpiresAt));
        }

        var (refreshToken, rawRefreshToken) = await refreshTokenIssuer.IssueAsync(user.Id, ct);

        await db.SaveChangesAsync(ct);

        await bus.PublishAsync(new UserLoggedInV1(
            user.Id.Value,
            user.Email.Value,
            cmd.IpAddress ?? string.Empty,
            Guid.NewGuid()));
        UsersTelemetry.EventsPublished.Add(1, new KeyValuePair<string, object?>("event", nameof(UserLoggedInV1)));

        var accessTokenExpiresAt = clock.UtcNow.AddMinutes(options.Value.AccessTokenLifetimeMinutes);
        var accessToken = jwtGenerator.Generate(user.Id, user.Email.Value, user.DisplayName, user.Role.Name, refreshToken.Id.Value);

        return LoginResponse.Authenticated(new LoginSessionResponse(
            user.Id.Value,
            accessToken,
            accessTokenExpiresAt,
            rawRefreshToken,
            refreshToken.ExpiresAt));
    }
}
