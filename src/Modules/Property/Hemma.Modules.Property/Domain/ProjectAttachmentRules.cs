namespace Hemma.Modules.Property.Domain;

public static class ProjectAttachmentRules
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> allowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static bool IsAllowed(string contentType, long size) =>
        size is > 0 and <= MaxSizeBytes && allowedContentTypes.Contains(contentType);
}
