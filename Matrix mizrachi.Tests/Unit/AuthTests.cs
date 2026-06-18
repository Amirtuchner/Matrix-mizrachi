using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Matrix_mizrachi.Tests.Unit;

public class AuthTests
{
    private const string Secret = "test-secret-key-minimum-32-characters!!";
    private const string Issuer = "matrix-mizrachi";
    private const string Audience = "matrix-mizrachi-client";

    private string GenerateToken(DateTime? expires = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(ClaimTypes.Name, "test-user")],
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters GetValidationParams(string? secret = null) =>
        new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? Secret)),
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

    [Fact]
    public void ValidToken_IsAccepted()
    {
        var tokenString = GenerateToken();
        var handler = new JwtSecurityTokenHandler();

        var principal = handler.ValidateToken(tokenString, GetValidationParams(), out var validatedToken);

        Assert.NotNull(principal);
        Assert.NotNull(validatedToken);
    }

    [Fact]
    public void ExpiredToken_ThrowsSecurityTokenExpiredException()
    {
        var tokenString = GenerateToken(expires: DateTime.UtcNow.AddHours(-1));
        var handler = new JwtSecurityTokenHandler();

        Assert.Throws<SecurityTokenExpiredException>(() =>
            handler.ValidateToken(tokenString, GetValidationParams(), out _));
    }

    [Fact]
    public void WrongSigningKey_ThrowsSecurityTokenException()
    {
        var tokenString = GenerateToken();
        var handler = new JwtSecurityTokenHandler();

        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(tokenString, GetValidationParams("wrong-key-minimum-32-characters!!"), out _));
    }

    [Fact]
    public void TokenContainsExpiryClaim()
    {
        var tokenString = GenerateToken();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Contains(token.Claims, c => c.Type == "exp");
    }

    [Fact]
    public void TokenUsesHS256Algorithm()
    {
        var tokenString = GenerateToken();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("HS256", token.Header.Alg);
    }
}
