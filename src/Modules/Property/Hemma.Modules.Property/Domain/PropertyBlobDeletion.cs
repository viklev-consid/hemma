using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Property.Domain;

public sealed class PropertyBlobDeletion : Entity<PropertyBlobDeletionId>
{
    private PropertyBlobDeletion(
        PropertyBlobDeletionId id,
        Guid householdId,
        string blobContainer,
        string blobKey,
        DateTimeOffset createdAt) : base(id)
    {
        HouseholdId = householdId;
        BlobContainer = blobContainer;
        BlobKey = blobKey;
        CreatedAt = createdAt;
    }

    private PropertyBlobDeletion() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public string BlobContainer { get; private set; } = string.Empty;
    public string BlobKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public int AttemptCount { get; private set; }

    public static PropertyBlobDeletion Create(Guid householdId, string blobContainer, string blobKey, DateTimeOffset createdAt) =>
        new(PropertyBlobDeletionId.New(), householdId, blobContainer, blobKey, createdAt);

    public void MarkAttempt(DateTimeOffset attemptedAt)
    {
        LastAttemptAt = attemptedAt;
        AttemptCount++;
    }
}
