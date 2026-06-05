using FluentValidation;

namespace Hemma.Modules.Economy.Features.RecordTransaction;

internal sealed class RecordTransactionValidator : AbstractValidator<RecordTransactionRequest>
{
    public RecordTransactionValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount.Amount).GreaterThan(0);
        RuleFor(x => x.Amount.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Kind).NotEmpty().Must(kind =>
            string.Equals(kind, "Expense", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(kind, "Income", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
