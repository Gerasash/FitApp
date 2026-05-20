using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FitApp.Api.Services;

/// <summary>
/// Генерация JWT для аутентифицированных пользователей.
///
/// Решение «вариант Б»: один access-токен на 30 дней, без refresh.
/// Этого достаточно для фитнес-приложения с редкими сессиями —
/// продлевать-обновлять токены каждые 15 минут было бы избыточной
/// сложностью без реальной пользы для безопасности.
/// </summary>
public class JwtService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _key;
    private readonly int _expirationDays;

    public JwtService(IConfiguration config)
    {
        _issuer = config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer не задан");
        _audience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience не задан");
        var secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret не задан");
        if (secret.Length < 32)
            throw new InvalidOperationException("Jwt:Secret должен быть не короче 32 символов");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _expirationDays = int.TryParse(config["Jwt:ExpirationDays"], out var d) ? d : 30;
    }

    public (string token, DateTime expiresAtUtc) CreateToken(long userId, string email)
    {
        var expires = DateTime.UtcNow.AddDays(_expirationDays);
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public TokenValidationParameters GetValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = _issuer,
        ValidAudience = _audience,
        IssuerSigningKey = _key,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
}
