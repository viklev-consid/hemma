namespace Hemma.Modules.Property.Jobs;

/// <summary>
/// Daily command that heals missing maintenance occurrences for active plans and fires
/// reminders for occurrences that have entered their plan's lead-time window.
/// <paramref name="Today"/> is overridable for tests; production passes null (uses the clock).
/// </summary>
public sealed record MaterializeMaintenanceOccurrences(DateOnly? Today = null);
