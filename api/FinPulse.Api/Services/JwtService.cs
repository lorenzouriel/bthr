using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FinPulse.Api.Services;

public interface IJwtService
{
    string GenerateToken(int userId, int plan = 0, bool isAdmin = false);
    int? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(int userId, int plan = 0, bool isAdmin = false)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("plan", plan.ToString()),
            new Claim("admin", isAdmin ? "true" : "false"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? ValidateToken(string token)
    {
        try
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenHandler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst("sub")?.Value;

            return userIdClaim != null ? int.Parse(userIdClaim) : null;
        }
        catch
        {
            return null;
        }
    }
}
