using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Portfolio.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("The current user does not have a valid subject claim.");
        }

        return userId;
    }
}
