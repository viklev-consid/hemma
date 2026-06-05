using FluentValidation;

namespace Hemma.Modules.Economy.Features.CategorizationRules;

internal sealed class CategorizationRuleValidator : AbstractValidator<CategorizationRuleRequest>
{
    public CategorizationRuleValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TargetCategoryId).NotEmpty();
        RuleFor(x => x.Match).NotEmpty().Must(x => string.Equals(x, "Contains", StringComparison.OrdinalIgnoreCase) || string.Equals(x, "Regex", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Pattern).NotEmpty().MaximumLength(200);
    }
}

internal sealed class SetCategorizationRuleEnabledValidator : AbstractValidator<SetCategorizationRuleEnabledRequest>
{
    public SetCategorizationRuleEnabledValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
    }
}
