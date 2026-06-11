using FluentValidation;

namespace Hemma.Modules.Economy.Features.CreateTransfer;

internal sealed class CreateTransferValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.ToAccountId).NotEmpty().NotEqual(x => x.FromAccountId);
        RuleFor(x => x.Amount.Amount).GreaterThan(0);
        RuleFor(x => x.Amount.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Mode).NotEmpty().Must(mode =>
            string.Equals(mode, "Neutral", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "Savings", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
