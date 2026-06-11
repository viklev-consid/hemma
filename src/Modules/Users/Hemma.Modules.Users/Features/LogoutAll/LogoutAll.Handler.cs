using ErrorOr;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Errors;
using Hemma.Modules.Users.Persistence;
using Hemma.Modules.Users.Security;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace Hemma.Modules.Users.Features.LogoutAll;

public sealed class LogoutAllHandler(UsersDbContext db, IRefreshTokenRevoker tokenRevoker, IMessageBus bus)
{
    public async Task<ErrorOr<LogoutAllResponse>> Handle(LogoutAllCommand cmd, CancellationToken ct)
        => await UsersTelemetry.InstrumentAsync(nameof(LogoutAllHandler), () => HandleCoreAsync(cmd, ct));

    private async Task<ErrorOr<LogoutAllResponse>> HandleCoreAsync(LogoutAllCommand cmd, CancellationToken ct)
    {
        var userId = new UserId(cmd.UserId);

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
        {
            return UsersErrors.UserNotFound;
        }

        await tokenRevoker.RevokeAllForUserAsync(userId, ct);

        await bus.PublishAsync(new UserLoggedOutAllDevicesV1(cmd.UserId, Guid.NewGuid()));
        UsersTelemetry.EventsPublished.Add(1, new KeyValuePair<string, object?>("event", nameof(UserLoggedOutAllDevicesV1)));

        return new LogoutAllResponse();
    }
}
