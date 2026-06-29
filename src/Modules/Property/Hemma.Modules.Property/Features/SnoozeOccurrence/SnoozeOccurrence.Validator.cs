using FluentValidation;

namespace Hemma.Modules.Property.Features.SnoozeOccurrence;

internal sealed class SnoozeOccurrenceValidator : AbstractValidator<SnoozeOccurrenceRequest>
{
    public SnoozeOccurrenceValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.SnoozedUntil).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(2000);
    }
}
