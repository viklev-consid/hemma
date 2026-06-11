namespace Hemma.Modules.Property.Domain;

public static class ProjectAttachmentRules
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const int MaxBlobContainerLength = 100;
    public const int MaxBlobKeyLength = 512;

    private static readonly HashSet<string> allowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static bool IsAllowed(string? contentType, long size) =>
        size is > 0 and <= MaxSizeBytes &&
        !string.IsNullOrWhiteSpace(contentType) &&
        allowedContentTypes.Contains(contentType);

    public static bool IsValidBlobReference(string? container, string? key) =>
        !string.IsNullOrWhiteSpace(container) &&
        container.Trim().Length <= MaxBlobContainerLength &&
        !string.IsNullOrWhiteSpace(key) &&
        key.Trim().Length <= MaxBlobKeyLength;
}
