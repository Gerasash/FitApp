using FitApp.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace FitApp.Api.Controllers;

/// <summary>
/// Проверочный endpoint — отвечает что сервис жив и БД доступна.
/// Используется Render для health check и для smoke-теста вручную.
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
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync();

            return Ok(new
            {
                status = "ok",
                serverTimeUtc = DateTime.UtcNow.ToString("o")
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
}
