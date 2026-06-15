using System.Text.Json;
using ErrorOr;
using Hemma.Modules.Property.Domain;
using Hemma.Modules.Property.Errors;
using Hemma.Modules.Property.Features.GetPropertyActivitySummary;
using Hemma.Modules.Property.Features.ListPropertyActivity;
using Hemma.Modules.Property.Persistence;
using Hemma.Shared.Kernel.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hemma.Modules.Property.Features.Shared;

public sealed class ActivityOperations(PropertyDbContext db, IClock clock, ICurrentUser currentUser)
{
    public ErrorOr<PropertyActivityEvent> Append(
        Guid householdId,
        PropertyActivityVerb verb,
        PropertyActivityTargetType targetType,
        Guid targetId,
        string summary,
        IReadOnlyDictionary<string, string?>? metadata = null)
    {
        var activity = PropertyActivityEvent.Create(
            householdId,
            CurrentActorId(),
            verb,
            targetType,
            targetId,
            summary,
            metadata,
            clock);
        if (activity.IsError)
        {
            return activity.Errors;
        }

        db.ActivityEvents.Add(activity.Value);
        return activity.Value;
    }

    public async Task<ErrorOr<ListPropertyActivityResponse>> ListPropertyActivityAsync(
        ListPropertyActivityQuery query,
        CancellationToken ct)
    {
        var activities = db.ActivityEvents
            .AsNoTracking()
            .Where(activity => activity.HouseholdId == query.HouseholdId);

        if (query.Since is not null)
        {
            activities = activities.Where(activity => activity.OccurredAt >= query.Since);
        }

        if (!string.IsNullOrWhiteSpace(query.TargetType))
        {
            var targetType = ParseTargetType(query.TargetType);
            if (targetType is null)
            {
                return PropertyErrors.ActivityTargetTypeInvalid;
            }

            activities = activities.Where(activity => activity.TargetType == targetType.Value);
        }

        if (query.TargetId is not null)
        {
            activities = activities.Where(activity => activity.TargetId == query.TargetId);
        }

        var limit = Math.Clamp(query.Limit ?? 50, 1, 100);
        var rows = await activities
            .OrderByDescending(activity => activity.OccurredAt)
            .ThenByDescending(activity => activity.Id)
            .Take(limit)
            .ToArrayAsync(ct);

        var items = rows
            .Select(activity => new PropertyActivityItemResponse(
                    activity.Id.Value,
                    activity.HouseholdId,
                    activity.OccurredAt,
                    activity.ActorId,
                    activity.Verb.ToString(),
                    activity.TargetType.ToString(),
                    activity.TargetId,
                    activity.Summary,
                    DeserializeMetadata(activity.MetadataJson)))
            .ToArray();

        return new ListPropertyActivityResponse(items);
    }

    public async Task<ErrorOr<PropertyActivitySummaryResponse>> GetPropertyActivitySummaryAsync(
        GetPropertyActivitySummaryQuery query,
        CancellationToken ct)
    {
        var activities = db.ActivityEvents
            .AsNoTracking()
            .Where(activity => activity.HouseholdId == query.HouseholdId);

        if (query.Since is not null)
        {
            activities = activities.Where(activity => activity.OccurredAt >= query.Since);
        }

        var verbRows = await activities
            .GroupBy(activity => activity.Verb)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToArrayAsync(ct);

        var targetTypeRows = await activities
            .GroupBy(activity => activity.TargetType)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToArrayAsync(ct);

        var byVerb = verbRows
            .Select(row => new PropertyActivityCountResponse(row.Key.ToString(), row.Count))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();

        var byTargetType = targetTypeRows
            .Select(row => new PropertyActivityCountResponse(row.Key.ToString(), row.Count))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();

        return new PropertyActivitySummaryResponse(query.HouseholdId, query.Since, byVerb, byTargetType);
    }

    private Guid? CurrentActorId() =>
        Guid.TryParse(currentUser.Id, out var userId) ? userId : null;

    private static PropertyActivityTargetType? ParseTargetType(string targetType) =>
        Enum.TryParse<PropertyActivityTargetType>(targetType, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : null;

    private static Dictionary<string, string?> DeserializeMetadata(string metadataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(metadataJson) ?? new Dictionary<string, string?>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string?>(StringComparer.Ordinal);
        }
    }
}
