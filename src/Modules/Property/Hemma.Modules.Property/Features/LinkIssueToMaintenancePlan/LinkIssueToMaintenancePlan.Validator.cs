using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.LinkIssueToMaintenancePlan;

internal sealed class LinkIssueToMaintenancePlanValidator : AbstractValidator<LinkIssueRequest>
{
    public LinkIssueToMaintenancePlanValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
    }
}
