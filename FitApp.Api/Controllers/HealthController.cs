using FitApp.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace FitApp.Api.Controllers;

/// <summary>
/// Проверочный endpoint — отвечает что сервис жив и SQLite доступен.
/// Нужен для smoke-теста локально и для Render health checks.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDb _db;

    public HealthController(AppDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            await using var conn = _db.OpenConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT StartedAtUtc FROM HealthCheck WHERE Id = 1";
            var startedAt = (string?)await cmd.ExecuteScalarAsync();

            return Ok(new
            {
                status = "ok",
                serverTimeUtc = DateTime.UtcNow.ToString("o"),
                dbStartedAtUtc = startedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
}
