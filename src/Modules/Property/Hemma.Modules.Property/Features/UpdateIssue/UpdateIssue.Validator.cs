using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.UpdateIssue;

internal sealed class UpdateIssueValidator : AbstractValidator<IssueRequest>
{
    public UpdateIssueValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Severity).MaximumLength(16).Must(BeSeverity).When(x => x.Severity is not null);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }

    private static bool BeSeverity(string? severity) =>
        Enum.TryParse<PropertyIssueSeverity>(severity, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
}
