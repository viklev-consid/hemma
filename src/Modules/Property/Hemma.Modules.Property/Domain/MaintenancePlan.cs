using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Property.Domain;

public sealed class MaintenancePlan : AggregateRoot<MaintenancePlanId>
{
    public const int MaxRecurrenceInterval = 120;
    public const int MaxLeadTimeDays = 365;

    private MaintenancePlan(
        MaintenancePlanId id,
        Guid householdId,
        string title,
        string? description,
        PropertyAreaId? areaId,
        MaintenanceRecurrenceUnit recurrenceUnit,
        int recurrenceInterval,
        DateOnly anchorDate,
        int leadTimeDays) : base(id)
    {
        HouseholdId = householdId;
        Title = title;
        Description = description;
        AreaId = areaId;
        RecurrenceUnit = recurrenceUnit;
        RecurrenceInterval = recurrenceInterval;
        AnchorDate = anchorDate;
        LeadTimeDays = leadTimeDays;
        IsActive = true;
    }

    private MaintenancePlan() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PropertyAreaId? AreaId { get; private set; }
    public MaintenanceRecurrenceUnit RecurrenceUnit { get; private set; }
    public int RecurrenceInterval { get; private set; }
    public DateOnly AnchorDate { get; private set; }
    public int LeadTimeDays { get; private set; }
    public bool IsActive { get; private set; }

    public static ErrorOr<MaintenancePlan> Create(
        Guid householdId,
        string title,
        string? description,
        PropertyAreaId? areaId,
        MaintenanceRecurrenceUnit recurrenceUnit,
        int recurrenceInterval,
        DateOnly anchorDate,
        int leadTimeDays)
    {
        var details = ValidateDetails(title, description, recurrenceUnit, recurrenceInterval, leadTimeDays);
        if (details.IsError)
        {
            return details.Errors;
        }

        return new MaintenancePlan(
            MaintenancePlanId.New(),
            householdId,
            details.Value.Title,
            details.Value.Description,
            areaId,
            recurrenceUnit,
            recurrenceInterval,
            anchorDate,
            leadTimeDays);
    }

    public ErrorOr<Success> UpdateDetails(
        string title,
        string? description,
        PropertyAreaId? areaId,
        MaintenanceRecurrenceUnit recurrenceUnit,
        int recurrenceInterval,
        DateOnly anchorDate,
        int leadTimeDays)
    {
        var details = ValidateDetails(title, description, recurrenceUnit, recurrenceInterval, leadTimeDays);
        if (details.IsError)
        {
            return details.Errors;
        }

        Title = details.Value.Title;
        Description = details.Value.Description;
        AreaId = areaId;
        RecurrenceUnit = recurrenceUnit;
        RecurrenceInterval = recurrenceInterval;
        AnchorDate = anchorDate;
        LeadTimeDays = leadTimeDays;
        return Result.Success;
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Returns the first occurrence date on or after <paramref name="floor"/> by stepping
    /// <see cref="AnchorDate"/> forward in <see cref="RecurrenceInterval"/> × <see cref="RecurrenceUnit"/>
    /// increments. No backfill — only the single next date is returned.
    /// </summary>
    public DateOnly NextDueOnOrAfter(DateOnly floor)
    {
        if (!HasValidRecurrenceForScheduling)
        {
            return floor;
        }

        var candidate = AnchorDate;
        var iterations = 0;
        while (candidate < floor)
        {
            if (++iterations > 2400)
            {
                return floor;
            }

            var next = candidate;
            candidate = RecurrenceUnit == MaintenanceRecurrenceUnit.Year
                ? TryAddYears(candidate, RecurrenceInterval)
                : TryAddMonths(candidate, RecurrenceInterval);

            if (candidate <= next)
            {
                return floor;
            }
        }

        return candidate;
    }

    public bool HasValidRecurrenceForScheduling =>
        Enum.IsDefined(RecurrenceUnit) && RecurrenceInterval >= 1;

    private static DateOnly TryAddMonths(DateOnly value, int months)
    {
        try
        {
            return value.AddMonths(months);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateOnly.MaxValue;
        }
    }

    private static DateOnly TryAddYears(DateOnly value, int years)
    {
        try
        {
            return value.AddYears(years);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateOnly.MaxValue;
        }
    }

    private static ErrorOr<PlanDetails> ValidateDetails(
        string title,
        string? description,
        MaintenanceRecurrenceUnit recurrenceUnit,
        int recurrenceInterval,
        int leadTimeDays)
    {
        var normalizedTitle = NormalizeRequired(title, 160);
        if (normalizedTitle is null)
        {
            return PropertyErrors.MaintenancePlanTitleInvalid;
        }

        var normalizedDescription = NormalizeOptional(description, 2000);
        if (normalizedDescription.IsError)
        {
            return PropertyErrors.MaintenancePlanDescriptionInvalid;
        }

        if (!Enum.IsDefined(recurrenceUnit) || recurrenceInterval < 1 || recurrenceInterval > MaxRecurrenceInterval)
        {
            return PropertyErrors.MaintenanceRecurrenceInvalid;
        }

        if (leadTimeDays < 0 || leadTimeDays > MaxLeadTimeDays)
        {
            return PropertyErrors.MaintenanceLeadTimeInvalid;
        }

        return new PlanDetails(normalizedTitle, normalizedDescription.Value.Value);
    }

    private static string? NormalizeRequired(string value, int maxLength)
    {
        var normalized = value.Trim();
        return normalized.Length is 0 || normalized.Length > maxLength ? null : normalized;
    }

    private static ErrorOr<OptionalString> NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new OptionalString(null);
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength ? PropertyErrors.MaintenancePlanDescriptionInvalid : new OptionalString(normalized);
    }

    private sealed record OptionalString(string? Value);

    private sealed record PlanDetails(string Title, string? Description);
}
