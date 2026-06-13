using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.CreateMaintenancePlan;

internal sealed class CreateMaintenancePlanValidator : AbstractValidator<MaintenancePlanRequest>
{
    public CreateMaintenancePlanValidator()
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
