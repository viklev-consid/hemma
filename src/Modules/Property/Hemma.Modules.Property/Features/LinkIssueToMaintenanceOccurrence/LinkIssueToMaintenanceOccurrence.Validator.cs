using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.LinkIssueToMaintenanceOccurrence;

internal sealed class LinkIssueToMaintenanceOccurrenceValidator : AbstractValidator<LinkIssueRequest>
{
    public LinkIssueToMaintenanceOccurrenceValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
    }
}
