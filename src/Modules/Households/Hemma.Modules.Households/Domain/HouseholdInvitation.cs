using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using Hemma.Modules.Households.Errors;
using Hemma.Shared.Kernel.Domain;
using Hemma.Shared.Kernel.Interfaces;

namespace Hemma.Modules.Households.Domain;

public sealed class HouseholdInvitation : Entity<HouseholdInvitationId>
{
    private HouseholdInvitation(
        HouseholdInvitationId id,
        HouseholdId householdId,
        string email,
        HouseholdRole role,
        byte[] tokenHash,
        DateTimeOffset invitedAt,
        DateTimeOffset expiresAt,
        Guid invitedByUserId) : base(id)
    {
        HouseholdId = householdId;
        Email = email;
        Role = role;
        TokenHash = tokenHash;
        InvitedAt = invitedAt;
        ExpiresAt = expiresAt;
        InvitedByUserId = invitedByUserId;
        IsPending = true;
    }

    private HouseholdInvitation() : base(default!) { }

    public HouseholdId HouseholdId { get; private set; } = null!;
    public string Email { get; private set; } = string.Empty;
    public HouseholdRole Role { get; private set; } = HouseholdRole.Member;
    public byte[] TokenHash { get; private set; } = [];
    public DateTimeOffset InvitedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? InvitedByUserId { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public Guid? AcceptedUserId { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public bool IsPending { get; private set; }

    public static ErrorOr<(HouseholdInvitation Invitation, string RawToken)> Create(
        HouseholdId householdId,
        string email,
        HouseholdRole role,
        TimeSpan lifetime,
        Guid invitedByUserId,
        IClock clock)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            return HouseholdsErrors.InvitationLifetimeInvalid;
        }

        var rawToken = GenerateRawToken();
        var now = clock.UtcNow;
        return (
            new HouseholdInvitation(
                HouseholdInvitationId.New(),
                householdId,
                email.Trim().ToLowerInvariant(),
                role,
                HashRawValue(rawToken),
                now,
                now.Add(lifetime),
                invitedByUserId),
            rawToken);
    }

    public ErrorOr<Success> Accept(Guid userId, string email, IClock clock)
    {
        // Both values are normalized to lowercase before comparison.
        if (!string.Equals(Email, email.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            return HouseholdsErrors.InvitationInvalid;
        }

        if (!CanBeAccepted(clock))
        {
            return HouseholdsErrors.InvitationInvalid;
        }

        AcceptedAt = clock.UtcNow;
        AcceptedUserId = userId;
        IsPending = false;
        return Result.Success;
    }

    public ErrorOr<Success> Revoke(Guid revokedByUserId, IClock clock)
    {
        if (AcceptedAt is not null)
        {
            return HouseholdsErrors.InvitationAlreadyAccepted;
        }

        if (RevokedAt is not null)
        {
            return HouseholdsErrors.InvitationAlreadyRevoked;
        }

        RevokedAt = clock.UtcNow;
        RevokedByUserId = revokedByUserId;
        IsPending = false;
        return Result.Success;
    }

    public bool CanBeAccepted(IClock clock) =>
        IsPending && ExpiresAt > clock.UtcNow;

    public void AnonymizeUserReferences(Guid userId)
    {
        if (InvitedByUserId == userId)
        {
            InvitedByUserId = null;
        }

        if (AcceptedUserId == userId)
        {
            AcceptedUserId = null;
        }

        if (RevokedByUserId == userId)
        {
            RevokedByUserId = null;
        }
    }

    public static byte[] HashRawValue(string rawValue) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(rawValue));

    private static string GenerateRawToken()
    {
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(rawBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
