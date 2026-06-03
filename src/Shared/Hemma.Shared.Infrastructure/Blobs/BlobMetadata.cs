namespace Hemma.Shared.Infrastructure.Blobs;

public sealed record BlobMetadata(string ContentType, long Length, string? FileName);
