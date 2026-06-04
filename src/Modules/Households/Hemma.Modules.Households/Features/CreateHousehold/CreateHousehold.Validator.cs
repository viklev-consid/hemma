using FluentValidation;

namespace Hemma.Modules.Households.Features.CreateHousehold;

internal sealed class CreateHouseholdValidator : AbstractValidator<CreateHouseholdRequest>
{
    public CreateHouseholdValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Slug).MaximumLength(100);
    }
}
