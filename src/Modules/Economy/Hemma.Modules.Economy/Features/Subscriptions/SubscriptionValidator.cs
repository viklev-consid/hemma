using FluentValidation;

namespace Hemma.Modules.Economy.Features.Subscriptions;

internal sealed class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.CadenceFrequency).NotEmpty();
        RuleFor(x => x.CadenceInterval).InclusiveBetween(1, 24);
        RuleFor(x => x.ChargeDay).InclusiveBetween(1, 28);
        RuleFor(x => x.ExpectedAmount).NotNull();
        RuleFor(x => x.LifecycleState).NotEmpty();
    }
}

internal sealed class ChangeLifecycleStateValidator : AbstractValidator<ChangeLifecycleStateRequest>
{
    public ChangeLifecycleStateValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.LifecycleState).NotEmpty();
    }
}

internal sealed class LinkTransactionValidator : AbstractValidator<LinkTransactionRequest>
{
    public LinkTransactionValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TransactionId).NotEmpty();
    }
}
