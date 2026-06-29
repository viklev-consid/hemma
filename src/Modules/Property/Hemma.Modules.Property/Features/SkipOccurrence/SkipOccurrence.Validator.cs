using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.SkipOccurrence;

internal sealed class SkipOccurrenceValidator : AbstractValidator<SkipOccurrenceRequest>
{
    public SkipOccurrenceValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
