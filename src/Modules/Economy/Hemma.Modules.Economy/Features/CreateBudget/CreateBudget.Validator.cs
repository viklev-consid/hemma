using FluentValidation;

namespace Hemma.Modules.Economy.Features.CreateBudget;

internal sealed class CreateBudgetValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetValidator()
    {
        RuleFor(request => request.HouseholdId).NotEmpty();
        RuleFor(request => request.AnchorDate).NotEmpty();
    }
}
