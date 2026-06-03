using Hemma.Shared.Infrastructure.Blobs;

namespace Hemma.Modules.Users.Avatars;

public sealed record StoredAvatar(BlobRef BlobRef, string ContentType, long SizeBytes, DateTimeOffset StoredAt);
