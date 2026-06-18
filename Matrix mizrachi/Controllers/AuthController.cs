using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Matrix_mizrachi.Controllers;

/// <summary>
/// Issues JWT tokens for testing purposes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates a JWT Bearer token valid for 1 hour.
    /// Use the returned token in the Authorization: Bearer {token} header.
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetToken()
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: [new Claim(ClaimTypes.Name, "test-user")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}
