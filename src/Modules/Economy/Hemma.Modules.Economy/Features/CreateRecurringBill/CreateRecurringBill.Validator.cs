using FluentValidation;

namespace Hemma.Modules.Economy.Features.CreateRecurringBill;

internal sealed class CreateRecurringBillValidator : AbstractValidator<CreateRecurringBillRequest>
{
    public CreateRecurringBillValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Amount.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Direction).NotEmpty();
        RuleFor(x => x.CadenceFrequency).NotEmpty();
        RuleFor(x => x.CadenceInterval).InclusiveBetween(1, 24);
        RuleFor(x => x.CadenceDayOfMonth).InclusiveBetween(1, 28);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
