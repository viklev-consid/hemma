using ErrorOr;
using Hemma.Modules.Users.Contracts.Events;
using Hemma.Modules.Users.Domain;
using Hemma.Modules.Users.Errors;
using Hemma.Modules.Users.Persistence;
using Hemma.Shared.Infrastructure.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Hemma.Modules.Users.Features.CreateInvitation;

public sealed class CreateInvitationHandler(
    UsersDbContext db,
    IOptions<UsersOptions> options,
    IMessageBus bus,
    IClock clock)
{
    public async Task<ErrorOr<CreateInvitationResponse>> Handle(CreateInvitationCommand cmd, CancellationToken ct)
        => await UsersTelemetry.InstrumentAsync(nameof(CreateInvitationHandler), () => HandleCoreAsync(cmd, ct));

    private async Task<ErrorOr<CreateInvitationResponse>> HandleCoreAsync(CreateInvitationCommand cmd, CancellationToken ct)
    {
        var emailResult = Email.Create(cmd.Email);
        if (emailResult.IsError)
        {
            return emailResult.Errors;
        }

        var email = emailResult.Value;

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            return UsersErrors.EmailAlreadyRegistered;
        }

        await db.UserInvitations
            .Where(i => i.Email == email.Value && i.IsPending && i.ExpiresAt <= clock.UtcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.IsPending, false), ct);

        var inviteResult = UserInvitation.Create(
            email,
            options.Value.Registration.InvitationTokenLifetime,
            clock,
            new UserId(cmd.InvitedByUserId),
            cmd.IpAddress,
            cmd.UserAgent);

        if (inviteResult.IsError)
        {
            return inviteResult.Errors;
        }

        var (invitation, rawToken) = inviteResult.Value;
        db.UserInvitations.Add(invitation);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            db.ChangeTracker.Clear();
            return UsersErrors.InvitationAlreadyExists;
        }

        await bus.PublishAsync(new UserInvitationCreatedV1(
            invitation.Id.Value,
            invitation.Email,
            rawToken,
            invitation.ExpiresAt,
            cmd.InvitedByUserId,
            Guid.NewGuid()));
        UsersTelemetry.EventsPublished.Add(1, new KeyValuePair<string, object?>("event", nameof(UserInvitationCreatedV1)));

        return new CreateInvitationResponse(invitation.Id.Value, invitation.Email, rawToken, invitation.ExpiresAt);
    }
}
