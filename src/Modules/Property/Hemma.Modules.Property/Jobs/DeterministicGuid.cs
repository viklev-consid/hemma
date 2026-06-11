using System.Security.Cryptography;

namespace Hemma.Modules.Property.Jobs;

/// <summary>
/// Derives a stable, reproducible <see cref="Guid"/> from two source GUIDs (SHA-256 based,
/// truncated to 128 bits). Used to build deterministic notification idempotency keys from
/// (occurrenceId, recipientUserId) so the daily materialise job never double-notifies. This is
/// a name-to-id mapping, not a security primitive.
/// </summary>
internal static class DeterministicGuid
{
    public static Guid Create(Guid first, Guid second)
    {
        Span<byte> input = stackalloc byte[32];
        first.TryWriteBytes(input[..16]);
        second.TryWriteBytes(input[16..]);

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);

        Span<byte> result = stackalloc byte[16];
        hash[..16].CopyTo(result);

        // Mark as a custom (version 8) UUID and set the RFC 4122 variant bits.
        result[6] = (byte)((result[6] & 0x0F) | 0x80);
        result[8] = (byte)((result[8] & 0x3F) | 0x80);

        return new Guid(result);
    }
}
