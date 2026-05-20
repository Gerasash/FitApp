using System.Net.Http.Json;

namespace FitApp.Services.Sync;

/// <summary>
/// Тонкая обёртка над /auth/register и /auth/login. JWT-токен хранится в
/// SecureStorage — это шифрованное хранилище платформы (Keychain на iOS,
/// EncryptedSharedPreferences на Android, DPAPI на Windows).
/// </summary>
public class AuthClient
{
    private const string TokenKey = "fitapp.sync.jwt";
    private const string EmailKey = "fitapp.sync.email";
    private const string UserIdKey = "fitapp.sync.userId";
    private const string ExpiresKey = "fitapp.sync.expires";

    private readonly HttpClient _http;

    public AuthClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<AuthResponse> RegisterAsync(string email, string password)
    {
        var resp = await _http.PostAsJsonAsync("auth/register", new AuthRequest(email, password));
        if (!resp.IsSuccessStatusCode)
        {
            var error = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Регистрация не удалась: {(int)resp.StatusCode}. {error}");
        }
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>()
                   ?? throw new InvalidOperationException("Пустой ответ /auth/register");
        await StoreAsync(auth);
        return auth;
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var resp = await _http.PostAsJsonAsync("auth/login", new AuthRequest(email, password));
        if (!resp.IsSuccessStatusCode)
        {
            var error = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Вход не удался: {(int)resp.StatusCode}. {error}");
        }
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>()
                   ?? throw new InvalidOperationException("Пустой ответ /auth/login");
        await StoreAsync(auth);
        return auth;
    }

    public async Task LogoutAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(ExpiresKey);
        await Task.CompletedTask;
    }

    /// <summary>Возвращает действующий JWT или null, если юзер не залогинен / токен истёк.</summary>
    public async Task<string?> GetTokenAsync()
    {
        var token = await SecureStorage.Default.GetAsync(TokenKey);
        if (string.IsNullOrEmpty(token)) return null;

        var expiresStr = await SecureStorage.Default.GetAsync(ExpiresKey);
        if (DateTime.TryParse(expiresStr, out var expires) && expires <= DateTime.UtcNow.AddMinutes(1))
        {
            // Просрочен — чистим, пусть юзер залогинится заново.
            await LogoutAsync();
            return null;
        }

        return token;
    }

    public async Task<string?> GetCurrentEmailAsync() => await SecureStorage.Default.GetAsync(EmailKey);

    private static async Task StoreAsync(AuthResponse auth)
    {
        await SecureStorage.Default.SetAsync("fitapp.sync.jwt", auth.Token);
        await SecureStorage.Default.SetAsync("fitapp.sync.email", auth.Email);
        await SecureStorage.Default.SetAsync("fitapp.sync.userId", auth.UserId.ToString());
        await SecureStorage.Default.SetAsync("fitapp.sync.expires", auth.ExpiresAtUtc.ToString("o"));
    }
}
