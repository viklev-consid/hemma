using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.Issues;

internal sealed class IssueRequestValidator : AbstractValidator<IssueRequest>
{
    public IssueRequestValidator()
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

internal sealed class ChangeIssueStatusRequestValidator : AbstractValidator<ChangeIssueStatusRequest>
{
    public ChangeIssueStatusRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().Must(status =>
            Enum.TryParse<PropertyIssueStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
    }
}

internal sealed class LinkIssueRequestValidator : AbstractValidator<LinkIssueRequest>
{
    public LinkIssueRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
    }
}

internal sealed class PromoteIssueToProjectRequestValidator : AbstractValidator<PromoteIssueToProjectRequest>
{
    public PromoteIssueToProjectRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).NotEmpty().Must(status =>
            Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
        RuleFor(x => x.Priority).MaximumLength(16).Must(priority =>
            Enum.TryParse<ProjectPriority>(priority, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            .When(x => x.Priority is not null);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.TargetEndDate)
            .GreaterThanOrEqualTo(x => x.TargetStartDate)
            .When(x => x.TargetStartDate is not null && x.TargetEndDate is not null);
    }
}
