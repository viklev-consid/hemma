using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Hemma.Modules.Users.Errors;
using Hemma.Modules.Users.Persistence;

namespace Hemma.Modules.Users.Features.GetUserById;

public sealed class GetUserByIdHandler(UsersDbContext db)
{
    public async Task<ErrorOr<GetUserByIdResponse>> Handle(GetUserByIdQuery query, CancellationToken ct)
        => await UsersTelemetry.InstrumentAsync(nameof(GetUserByIdHandler), () => HandleCoreAsync(query, ct));

    private async Task<ErrorOr<GetUserByIdResponse>> HandleCoreAsync(GetUserByIdQuery query, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == query.UserId, ct);

        if (user is null)
        {
            return UsersErrors.UserNotFound;
        }

        return new GetUserByIdResponse(
            user.Id.Value,
            user.Email.Value,
            user.DisplayName,
            user.Role.Name,
            user.CreatedAt,
            HasCompletedOnboarding: user.HasCompletedOnboarding);
    }
}
