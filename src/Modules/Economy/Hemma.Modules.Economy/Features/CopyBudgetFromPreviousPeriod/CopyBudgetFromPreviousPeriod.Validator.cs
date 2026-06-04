using FluentValidation;

namespace Hemma.Modules.Economy.Features.CopyBudgetFromPreviousPeriod;

internal sealed class CopyBudgetFromPreviousPeriodValidator : AbstractValidator<CopyBudgetFromPreviousPeriodRequest>
{
    public CopyBudgetFromPreviousPeriodValidator()
    {
        RuleFor(request => request.HouseholdId).NotEmpty();
        RuleFor(request => request.AnchorDate).NotEmpty();
    }
}
