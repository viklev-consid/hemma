using FluentValidation;

namespace Hemma.Modules.Users.Features.RefreshToken;

internal sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
