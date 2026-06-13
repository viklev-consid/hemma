using FluentValidation;

namespace Hemma.Modules.Property.Features.PromoteOccurrenceToProject;

internal sealed class PromoteOccurrenceToProjectValidator : AbstractValidator<PromoteOccurrenceRequest>
{
    public PromoteOccurrenceToProjectValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.Priority).MaximumLength(40);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
