using FluentValidation;

namespace Hemma.Modules.Users.Features.ConfirmEmailChange;

internal sealed class ConfirmEmailChangeValidator : AbstractValidator<ConfirmEmailChangeRequest>
{
    public ConfirmEmailChangeValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
