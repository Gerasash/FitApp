using System.Net;
using System.Text.Json;

namespace FitApp.Services.Sync;

/// <summary>
/// Утилиты для превращения сетевых исключений в человеко-читаемые сообщения.
/// Render free tier «засыпает» через 15 мин неактивности — первый запрос
/// после сна занимает ~30 сек (cold start), это надо подсказывать юзеру.
/// </summary>
public static class NetworkErrors
{
    /// <summary>Маппит исключение в короткое сообщение на русском.</summary>
    public static string Friendly(Exception ex)
    {
        return ex switch
        {
            TaskCanceledException => "Сервер не отвечает. Попробуй ещё раз через минуту.",
            HttpRequestException hre when hre.InnerException is System.Net.Sockets.SocketException
                => "Нет связи с сервером. Проверь интернет.",
            HttpRequestException
                => "Нет связи с сервером. Проверь интернет.",
            InvalidOperationException => ex.Message,
            _ => "Что-то пошло не так: " + ex.Message
        };
    }

    /// <summary>
    /// Парсит тело ответа от /auth/* в читаемое сообщение. Сервер отдаёт
    /// { "error": "..." } либо просто текст. Если ничего не вышло —
    /// возвращает дефолтный текст исходя из HTTP-кода.
    /// </summary>
    public static string ParseAuthError(HttpStatusCode status, string body)
    {
        // Попытка вытащить поле "error" / "message"
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                return err.GetString() ?? FallbackAuth(status);
            if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                return msg.GetString() ?? FallbackAuth(status);
        }
        catch
        {
            // не JSON — игнорируем
        }
        return FallbackAuth(status);
    }

    private static string FallbackAuth(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Неверный email или пароль.",
        HttpStatusCode.Conflict => "Такой email уже зарегистрирован.",
        HttpStatusCode.BadRequest => "Неверные данные. Проверь email и пароль (≥6 символов).",
        HttpStatusCode.InternalServerError => "Ошибка сервера. Попробуй позже.",
        _ => $"Ошибка сервера ({(int)status})."
    };
}
