using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.UpdateTag;

internal sealed class UpdateTagValidator : AbstractValidator<PropertyTagRequest>
{
    public UpdateTagValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).MaximumLength(40);
    }
}
