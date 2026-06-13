using System.Security.Cryptography;
using System.Text;

namespace Hemma.Modules.Property.Jobs;

/// <summary>
/// Derives a stable, reproducible <see cref="Guid"/> from notification identity parts
/// (SHA-256 based, truncated to 128 bits). This is a name-to-id mapping, not a security primitive.
/// </summary>
internal static class DeterministicGuid
{
    public static Guid Create(Guid first, Guid second)
    {
        Span<byte> input = stackalloc byte[32];
        first.TryWriteBytes(input[..16]);
        second.TryWriteBytes(input[16..]);

        return CreateFromBytes(input);
    }

    public static Guid Create(params string[] parts)
    {
        var input = Encoding.UTF8.GetBytes(string.Join('\u001f', parts));
        return CreateFromBytes(input);
    }

    private static Guid CreateFromBytes(ReadOnlySpan<byte> input)
    {
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
