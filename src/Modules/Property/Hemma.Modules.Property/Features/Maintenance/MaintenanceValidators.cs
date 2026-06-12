using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.Maintenance;

internal sealed class MaintenancePlanRequestValidator : AbstractValidator<MaintenancePlanRequest>
{
    public MaintenancePlanRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.RecurrenceUnit).NotEmpty().Must(BeRecurrenceUnit);
        RuleFor(x => x.RecurrenceInterval).InclusiveBetween(1, MaintenancePlan.MaxRecurrenceInterval);
        RuleFor(x => x.LeadTimeDays).InclusiveBetween(0, MaintenancePlan.MaxLeadTimeDays);
    }

    private static bool BeRecurrenceUnit(string unit) =>
        Enum.TryParse<MaintenanceRecurrenceUnit>(unit, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
}

internal sealed class PromoteOccurrenceRequestValidator : AbstractValidator<PromoteOccurrenceRequest>
{
    public PromoteOccurrenceRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).MaximumLength(16).Must(BeProjectPriority).When(x => x.Priority is not null);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.Status).NotEmpty().Must(BeProjectStatus);
        RuleFor(x => x.TargetEndDate)
            .GreaterThanOrEqualTo(x => x.TargetStartDate)
            .When(x => x.TargetStartDate is not null && x.TargetEndDate is not null);
    }

    private static bool BeProjectStatus(string status) =>
        Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);

    private static bool BeProjectPriority(string? priority) =>
        Enum.TryParse<ProjectPriority>(priority, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
}

internal sealed class CompleteOccurrenceRequestValidator : AbstractValidator<CompleteOccurrenceRequest>
{
    public CompleteOccurrenceRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class SkipOccurrenceRequestValidator : AbstractValidator<SkipOccurrenceRequest>
{
    public SkipOccurrenceRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
