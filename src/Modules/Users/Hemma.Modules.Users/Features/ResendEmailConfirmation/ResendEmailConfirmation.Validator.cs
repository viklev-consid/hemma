using FluentValidation;

namespace Hemma.Modules.Users.Features.ResendEmailConfirmation;

internal sealed class ResendEmailConfirmationValidator : AbstractValidator<ResendEmailConfirmationRequest>
{
    public ResendEmailConfirmationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }
}
