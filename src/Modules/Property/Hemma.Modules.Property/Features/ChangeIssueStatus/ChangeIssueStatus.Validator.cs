using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.ChangeIssueStatus;

internal sealed class ChangeIssueStatusValidator : AbstractValidator<ChangeIssueStatusRequest>
{
    public ChangeIssueStatusValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().Must(status =>
            Enum.TryParse<PropertyIssueStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
    }
}
