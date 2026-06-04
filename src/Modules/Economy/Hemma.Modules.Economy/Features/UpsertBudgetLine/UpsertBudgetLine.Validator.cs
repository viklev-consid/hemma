using FluentValidation;

namespace Hemma.Modules.Economy.Features.UpsertBudgetLine;

internal sealed class UpsertBudgetLineValidator : AbstractValidator<UpsertBudgetLineRequest>
{
    public UpsertBudgetLineValidator()
    {
        RuleFor(request => request.HouseholdId).NotEmpty();
        RuleFor(request => request.BudgetId).NotEmpty();
        RuleFor(request => request.CategoryId).NotEmpty();
        RuleFor(request => request.Amount).NotNull();
        RuleFor(request => request.Amount.Amount).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Amount.Currency).NotEmpty().Length(3);
    }
}
