using System.ComponentModel.DataAnnotations;

namespace FitApp.Api.Dtos;

/// <summary>Запрос регистрации нового пользователя.</summary>
public record RegisterRequest(
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(6), MaxLength(100)] string Password);

/// <summary>Запрос входа.</summary>
public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

/// <summary>Ответ после успешной регистрации/входа: JWT и базовые сведения о юзере.</summary>
public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    long UserId,
    string Email);
