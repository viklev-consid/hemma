using FluentValidation;

namespace Hemma.Modules.Property.Features.Projects;

internal sealed class ProjectRequestValidator : AbstractValidator<ProjectRequest>
{
    public ProjectRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).NotEmpty().Must(BeProjectStatus);
        RuleFor(x => x.Area).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.TargetEndDate)
            .GreaterThanOrEqualTo(x => x.TargetStartDate)
            .When(x => x.TargetStartDate is not null && x.TargetEndDate is not null);
    }

    private static bool BeProjectStatus(string status) =>
        Enum.TryParse<Domain.ProjectStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
}

internal sealed class ChangeProjectStatusRequestValidator : AbstractValidator<ChangeProjectStatusRequest>
{
    public ChangeProjectStatusRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().Must(status =>
            Enum.TryParse<Domain.ProjectStatus>(status, true, out var parsed) && Enum.IsDefined(parsed));
    }
}

internal sealed class ProjectTaskRequestValidator : AbstractValidator<ProjectTaskRequest>
{
    public ProjectTaskRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).NotEmpty().Must(status =>
            Enum.TryParse<Domain.ProjectTaskStatus>(status, true, out var parsed) && Enum.IsDefined(parsed));
    }
}

internal sealed class ReorderTasksRequestValidator : AbstractValidator<ReorderTasksRequest>
{
    public ReorderTasksRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TaskIds).NotEmpty();
    }
}

internal sealed class ProjectLinkRequestValidator : AbstractValidator<ProjectLinkRequest>
{
    public ProjectLinkRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048).Must(BeHttpUrl);
    }

    private static bool BeHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
