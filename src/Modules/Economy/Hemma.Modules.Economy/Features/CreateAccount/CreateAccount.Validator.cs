using FluentValidation;

namespace Hemma.Modules.Economy.Features.CreateAccount;

internal sealed class CreateAccountValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountValidator()
    {
        RuleFor(request => request.HouseholdId).NotEmpty();
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Type).NotEmpty();
        RuleFor(request => request.OpeningBalance).NotNull();
        RuleFor(request => request.OpeningBalance.Amount).GreaterThanOrEqualTo(0);
        RuleFor(request => request.OpeningBalance.Currency).NotEmpty().Length(3);
    }
}
