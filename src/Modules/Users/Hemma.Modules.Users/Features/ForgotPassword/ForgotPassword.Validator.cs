using FluentValidation;

namespace Hemma.Modules.Users.Features.ForgotPassword;

internal sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
