using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;
using System.Text.Json;

namespace Hemma.Modules.Property.Domain;

public sealed class PropertyActivityEvent : Entity<PropertyActivityEventId>
{
    private PropertyActivityEvent(
        PropertyActivityEventId id,
        Guid householdId,
        DateTimeOffset occurredAt,
        Guid? actorId,
        PropertyActivityVerb verb,
        PropertyActivityTargetType targetType,
        Guid targetId,
        string summary,
        string metadataJson) : base(id)
    {
        HouseholdId = householdId;
        OccurredAt = occurredAt;
        ActorId = actorId;
        Verb = verb;
        TargetType = targetType;
        TargetId = targetId;
        Summary = summary;
        MetadataJson = metadataJson;
    }

    private PropertyActivityEvent() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? ActorId { get; private set; }
    public PropertyActivityVerb Verb { get; private set; }
    public PropertyActivityTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string MetadataJson { get; private set; } = "{}";

    public static ErrorOr<PropertyActivityEvent> Create(
        Guid householdId,
        Guid? actorId,
        PropertyActivityVerb verb,
        PropertyActivityTargetType targetType,
        Guid targetId,
        string summary,
        IReadOnlyDictionary<string, string?>? metadata,
        IClock clock)
    {
        var normalizedSummary = summary.Trim();
        if (normalizedSummary.Length is 0 or > 240)
        {
            return PropertyErrors.ActivitySummaryInvalid;
        }

        if (!Enum.IsDefined(verb))
        {
            return PropertyErrors.ActivityVerbInvalid;
        }

        if (!Enum.IsDefined(targetType))
        {
            return PropertyErrors.ActivityTargetTypeInvalid;
        }

        return new PropertyActivityEvent(
            PropertyActivityEventId.New(),
            householdId,
            clock.UtcNow,
            actorId,
            verb,
            targetType,
            targetId,
            normalizedSummary,
            JsonSerializer.Serialize(metadata ?? new Dictionary<string, string?>(StringComparer.Ordinal)));
    }
}
