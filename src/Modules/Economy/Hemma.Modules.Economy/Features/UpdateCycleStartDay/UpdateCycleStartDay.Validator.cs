using FluentValidation;

namespace Hemma.Modules.Economy.Features.UpdateCycleStartDay;

internal sealed class UpdateCycleStartDayValidator : AbstractValidator<UpdateCycleStartDayRequest>
{
    public UpdateCycleStartDayValidator()
    {
        RuleFor(request => request.HouseholdId).NotEmpty();
        RuleFor(request => request.CycleStartDay).InclusiveBetween(1, 28);
    }
}
