using FluentValidation;

namespace Hemma.Modules.Economy.Features.ConfirmEstimatedBill;

internal sealed class ConfirmEstimatedBillValidator : AbstractValidator<ConfirmEstimatedBillRequest>
{
    public ConfirmEstimatedBillValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.Amount.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Amount.Currency).NotEmpty().Length(3);
    }
}
