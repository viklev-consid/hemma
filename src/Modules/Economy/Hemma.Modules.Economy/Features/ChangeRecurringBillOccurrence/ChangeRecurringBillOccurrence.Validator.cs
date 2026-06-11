using FluentValidation;

namespace Hemma.Modules.Economy.Features.ChangeRecurringBillOccurrence;

internal sealed class ChangeRecurringBillOccurrenceValidator : AbstractValidator<ChangeRecurringBillOccurrenceRequest>
{
    public ChangeRecurringBillOccurrenceValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
    }
}
