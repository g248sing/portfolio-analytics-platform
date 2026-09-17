using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Auth;
using Portfolio.Application.Auth.Dtos;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Identity;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    PortfolioDbContext dbContext) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    public async Task<ActionResult<AccessTokenResponse>> Register(RegisterRequest request)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict(new ProblemDetails { Title = "An account with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return await IssueTokensAsync(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccessTokenResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid email or password." });
        }

        return await IssueTokensAsync(user);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AccessTokenResponse>> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rawToken) || string.IsNullOrEmpty(rawToken))
        {
            return Unauthorized(new ProblemDetails { Title = "Missing refresh token." });
        }

        var tokenHash = tokenService.HashRefreshToken(rawToken);
        var existingToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (existingToken is null || !existingToken.IsActive)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid or expired refresh token." });
        }

        var user = await userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user is null)
        {
            return Unauthorized(new ProblemDetails { Title = "Invalid refresh token." });
        }

        existingToken.RevokedAt = DateTimeOffset.UtcNow;

        var response = await IssueTokensAsync(user, existingToken);
        return response;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var rawToken) && !string.IsNullOrEmpty(rawToken))
        {
            var tokenHash = tokenService.HashRefreshToken(rawToken);
            var existingToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash);
            if (existingToken is not null)
            {
                existingToken.RevokedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete(RefreshTokenCookieName);
        return NoContent();
    }

    private async Task<AccessTokenResponse> IssueTokensAsync(ApplicationUser user, RefreshToken? replaces = null)
    {
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user.Id, user.Email!);

        var rawRefreshToken = tokenService.GenerateRefreshTokenValue();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(rawRefreshToken),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(tokenService.RefreshTokenLifetime),
        };

        dbContext.RefreshTokens.Add(refreshToken);

        if (replaces is not null)
        {
            replaces.ReplacedByTokenId = refreshToken.Id;
        }

        await dbContext.SaveChangesAsync();

        var isDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        Response.Cookies.Append(RefreshTokenCookieName, rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            // Frontend and API are deployed on different subdomains of a shared-hosting
            // domain (e.g. onrender.com), which browsers treat as genuinely different
            // sites — a SameSite=Strict/Lax cookie set by the API is never sent back on
            // the frontend's cross-site requests to it. SameSite=None (which requires
            // Secure=true, hence only outside Development) is what actually works here;
            // locally, different localhost ports still count as the same site, so Strict
            // is fine and stronger there.
            SameSite = isDevelopment ? SameSiteMode.Strict : SameSiteMode.None,
            Path = "/api/auth",
            Expires = refreshToken.ExpiresAt,
        });

        return new AccessTokenResponse(accessToken, expiresAt);
    }
}
