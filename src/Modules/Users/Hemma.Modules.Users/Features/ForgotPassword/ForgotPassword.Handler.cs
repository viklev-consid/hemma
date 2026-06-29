using ErrorOr;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Persistence;
using Hemma.Modules.Users.Security;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Hemma.Modules.Users.Features.ForgotPassword;

public sealed class ForgotPasswordHandler(
    UsersDbContext db,
    ISingleUseTokenService tokenService,
    IOptions<UsersOptions> options,
    IClock clock,
    IMessageBus bus)
{
    public async Task<ErrorOr<ForgotPasswordResponse>> Handle(ForgotPasswordCommand cmd, CancellationToken ct)
        => await UsersTelemetry.InstrumentAsync(nameof(ForgotPasswordHandler), () => HandleCoreAsync(cmd, ct));

    private async Task<ErrorOr<ForgotPasswordResponse>> HandleCoreAsync(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var emailResult = Email.Create(cmd.Email);

        // Always return the same response regardless of whether the email exists.
        if (emailResult.IsError)
        {
            return new ForgotPasswordResponse();
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailResult.Value, ct);

        if (user is not null)
        {
            await db.SingleUseTokens
                .Where(t => t.UserId == user.Id &&
                            t.Purpose == TokenPurpose.PasswordReset &&
                            t.ConsumedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ConsumedAt, clock.UtcNow), ct);

            var (_, rawToken) = tokenService.Create(
                user.Id,
                TokenPurpose.PasswordReset,
                options.Value.PasswordResetTokenLifetime);

            await db.SaveChangesAsync(ct);

            await bus.PublishAsync(new PasswordResetRequestedV1(
                user.Id.Value,
                user.Email.Value,
                rawToken,
                Guid.NewGuid()));
            UsersTelemetry.EventsPublished.Add(1, new KeyValuePair<string, object?>("event", nameof(PasswordResetRequestedV1)));
        }

        return new ForgotPasswordResponse();
    }
}
