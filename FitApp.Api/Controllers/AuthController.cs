using FitApp.Api.Data;
using FitApp.Api.Dtos;
using FitApp.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace FitApp.Api.Controllers;

/// <summary>
/// Регистрация и вход. JWT-токен выдаём сразу после регистрации, чтобы
/// клиенту не приходилось делать второй вызов login.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDb _db;
    private readonly JwtService _jwt;

    public AuthController(AppDb db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        await using var conn = _db.OpenConnection();

        // Проверяем уникальность email (без учёта регистра)
        await using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT 1 FROM Users WHERE LOWER(Email) = LOWER(@email)";
            check.Parameters.AddWithValue("@email", req.Email);
            var exists = await check.ExecuteScalarAsync();
            if (exists != null)
                return Conflict(new { error = "Пользователь с таким email уже зарегистрирован." });
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        await using var insert = conn.CreateCommand();
        insert.CommandText = @"
            INSERT INTO Users (Email, PasswordHash, CreatedAtUtc)
            VALUES (@email, @hash, @created)
            RETURNING Id;";
        insert.Parameters.AddWithValue("@email", req.Email.ToLowerInvariant());
        insert.Parameters.AddWithValue("@hash", hash);
        insert.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o"));
        var userId = (long)(await insert.ExecuteScalarAsync())!;

        var (token, expires) = _jwt.CreateToken(userId, req.Email);
        return Ok(new AuthResponse(token, expires, userId, req.Email));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        await using var conn = _db.OpenConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Email, PasswordHash FROM Users WHERE LOWER(Email) = LOWER(@email)";
        cmd.Parameters.AddWithValue("@email", req.Email);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Unauthorized(new { error = "Неверный email или пароль." });

        var userId = reader.GetInt64(0);
        var email = reader.GetString(1);
        var hash = reader.GetString(2);

        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash))
            return Unauthorized(new { error = "Неверный email или пароль." });

        var (token, expires) = _jwt.CreateToken(userId, email);
        return Ok(new AuthResponse(token, expires, userId, email));
    }
}
