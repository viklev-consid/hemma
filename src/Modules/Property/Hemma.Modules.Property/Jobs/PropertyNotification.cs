using Hemma.Modules.Notifications.Contracts.Dtos;

namespace Hemma.Modules.Property.Jobs;

public sealed record PropertyNotification(
    string Source,
    Guid SourceId,
    Guid HouseholdId,
    string Kind,
    DateOnly RelevantDate,
    string Type,
    NotificationSeverity Severity,
    string Title,
    string Body,
    NotificationLinkDto? Link);
