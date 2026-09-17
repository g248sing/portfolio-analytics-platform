namespace Portfolio.Application.Auth.Dtos;

public record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
