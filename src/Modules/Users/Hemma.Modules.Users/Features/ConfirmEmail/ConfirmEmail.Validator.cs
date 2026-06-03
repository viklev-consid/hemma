using FluentValidation;

namespace Hemma.Modules.Users.Features.ConfirmEmail;

internal sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
