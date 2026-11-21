using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MinisterDiary.Api.Models;

namespace MinisterDiary.Api.Services;

public interface IJwtService
{
    (string token, DateTime expiresAt) GenerateToken(User user, bool rememberMe);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public (string token, DateTime expiresAt) GenerateToken(User user, bool rememberMe)
    {
        var jwtKey = _config["Jwt:Key"] ?? "MinisterDiary_SuperSecret_JwtKey_2026_HighSecurityKey!";
        var jwtIssuer = _config["Jwt:Issuer"] ?? "MinisterDiaryApi";
        var jwtAudience = _config["Jwt:Audience"] ?? "MinisterDiaryClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // A1 spec: Token lasts 1 day, or 30 days with Remember me
        var expiresAt = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(1);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(ClaimTypes.Name, user.FullName),
            new("initials", user.Initials)
        };

        if (!string.IsNullOrEmpty(user.Portfolio))
            claims.Add(new("portfolio", user.Portfolio));

        if (!string.IsNullOrEmpty(user.Designation))
            claims.Add(new("designation", user.Designation));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiresAt);
    }
}
