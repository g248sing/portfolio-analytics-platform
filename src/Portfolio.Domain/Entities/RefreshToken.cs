namespace Portfolio.Domain.Entities;

/// <summary>
/// A rotating refresh token issued to a user's session. Only the SHA-256 hash of
/// the token value is stored; the raw value lives solely in the client's
/// httpOnly cookie.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    // Set when this token is rotated out in favor of a new one, forming an
    // audit chain and letting reuse-of-a-revoked-token be detected as a
    // possible token theft signal.
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
