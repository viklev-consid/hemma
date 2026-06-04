using FluentValidation;

namespace Hemma.Modules.Economy.Features.CreateEconomySettings;

internal sealed class CreateEconomySettingsValidator : AbstractValidator<CreateEconomySettingsRequest>
{
    public CreateEconomySettingsValidator()
    {
        RuleFor(request => request.HouseholdId).NotEmpty();
        RuleFor(request => request.CycleStartDay).InclusiveBetween(1, 28);
        RuleFor(request => request.DefaultCurrency).NotEmpty().Length(3);
    }
}
