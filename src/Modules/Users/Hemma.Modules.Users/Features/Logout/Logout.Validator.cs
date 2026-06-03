using FluentValidation;

namespace Hemma.Modules.Users.Features.Logout;

internal sealed class LogoutValidator : AbstractValidator<LogoutRequest>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
