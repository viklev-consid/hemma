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

    private static readonly byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsAllowed(string? contentType, long size) =>
        size is > 0 and <= MaxSizeBytes &&
        !string.IsNullOrWhiteSpace(contentType) &&
        allowedContentTypes.Contains(contentType);

    public static bool HasAllowedSignature(string? contentType, ReadOnlySpan<byte> content)
    {
        if (!IsAllowed(contentType, content.Length))
        {
            return false;
        }

        return contentType?.ToLowerInvariant() switch
        {
            "application/pdf" => content.StartsWith("%PDF-"u8),
            "image/jpeg" => content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            "image/png" => content.StartsWith(pngSignature),
            "image/webp" => content.Length >= 12 &&
                content[..4].SequenceEqual("RIFF"u8) &&
                content[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    public static bool IsValidBlobReference(string? container, string? key) =>
        !string.IsNullOrWhiteSpace(container) &&
        container.Trim().Length <= MaxBlobContainerLength &&
        !string.IsNullOrWhiteSpace(key) &&
        key.Trim().Length <= MaxBlobKeyLength;
}
