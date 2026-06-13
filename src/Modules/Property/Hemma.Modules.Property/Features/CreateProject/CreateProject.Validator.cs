using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.CreateProject;

internal sealed class CreateProjectValidator : AbstractValidator<ProjectRequest>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).NotEmpty().Must(BeProjectStatus);
        RuleFor(x => x.Priority).MaximumLength(16).Must(BeProjectPriority).When(x => x.Priority is not null);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.TargetEndDate)
            .GreaterThanOrEqualTo(x => x.TargetStartDate)
            .When(x => x.TargetStartDate is not null && x.TargetEndDate is not null);
    }

    private static bool BeProjectStatus(string status) =>
        Enum.TryParse<Domain.ProjectStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);

    private static bool BeProjectPriority(string? priority) =>
        Enum.TryParse<Domain.ProjectPriority>(priority, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
}
