using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.CreateTag;

internal sealed class CreateTagValidator : AbstractValidator<PropertyTagRequest>
{
    public CreateTagValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).MaximumLength(40);
    }
}
