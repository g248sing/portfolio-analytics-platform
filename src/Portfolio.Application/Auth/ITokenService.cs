namespace Portfolio.Application.Auth;

public interface ITokenService
{
    (string AccessToken, DateTimeOffset ExpiresAt) CreateAccessToken(Guid userId, string email);

    string GenerateRefreshTokenValue();

    string HashRefreshToken(string rawToken);

    TimeSpan RefreshTokenLifetime { get; }
}
