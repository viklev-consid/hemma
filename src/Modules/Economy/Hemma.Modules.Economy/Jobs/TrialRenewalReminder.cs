namespace Hemma.Modules.Economy.Jobs;

public sealed record TrialRenewalReminder(DateOnly? Today = null, int DaysAhead = 3);
