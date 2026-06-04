using FluentValidation;

namespace Hemma.Modules.Households.Features.UpdateHousehold;

internal sealed class UpdateHouseholdValidator : AbstractValidator<UpdateHouseholdRequest>
{
    public UpdateHouseholdValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Slug).NotEmpty().MaximumLength(100);
    }
}
