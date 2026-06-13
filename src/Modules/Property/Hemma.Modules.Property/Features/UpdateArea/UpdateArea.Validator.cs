using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.UpdateArea;

internal sealed class UpdateAreaValidator : AbstractValidator<PropertyAreaRequest>
{
    public UpdateAreaValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
