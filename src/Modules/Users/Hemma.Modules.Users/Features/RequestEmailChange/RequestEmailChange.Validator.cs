using FluentValidation;

namespace Hemma.Modules.Users.Features.RequestEmailChange;

internal sealed class RequestEmailChangeValidator : AbstractValidator<RequestEmailChangeRequest>
{
    public RequestEmailChangeValidator()
    {
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.CurrentPassword).NotEmpty();
    }
}
