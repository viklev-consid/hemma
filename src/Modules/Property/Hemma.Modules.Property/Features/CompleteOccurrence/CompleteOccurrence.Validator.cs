using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.CompleteOccurrence;

internal sealed class CompleteOccurrenceValidator : AbstractValidator<CompleteOccurrenceRequest>
{
    public CompleteOccurrenceValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
