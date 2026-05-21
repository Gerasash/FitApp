using System.ComponentModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Services.Sync;

namespace FitApp.ViewModels;

/// <summary>
/// ViewModel страницы «Аккаунт» (этап 6).
///
/// Состояние UI собрано вокруг одной булевой переменной IsLoggedIn:
/// если пользователь авторизован — показываем синхронизацию и кнопку
/// выйти; если нет — форму входа/регистрации.
/// </summary>
public partial class AccountViewModel : ObservableObject
{
    private readonly AuthClient _auth;
    private readonly SyncService _sync;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedOut))]
    private bool isLoggedIn;

    public bool IsLoggedOut => !IsLoggedIn;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string statusText = "";

    public bool HasStatus => !string.IsNullOrEmpty(StatusText);

    [ObservableProperty] private string lastSyncText = "";

    public AccountViewModel(AuthClient auth, SyncService sync)
    {
        _auth = auth;
        _sync = sync;
    }

    /// <summary>Вызвать при появлении страницы — подтянуть текущее состояние из SecureStorage.</summary>
    public async Task InitAsync()
    {
        var token = await _auth.GetTokenAsync();
        if (token != null)
        {
            IsLoggedIn = true;
            Email = await _auth.GetCurrentEmailAsync() ?? "";
            UpdateLastSyncText();
        }
        else
        {
            IsLoggedIn = false;
            Email = "";
            Password = "";
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            StatusText = "Заполните email и пароль.";
            return;
        }
        IsBusy = true;
        StatusText = "Вход...";
        using var coldStart = StartColdStartHint();
        try
        {
            await _auth.LoginAsync(Email.Trim(), Password);
            IsLoggedIn = true;
            Password = "";
            StatusText = "Готово.";
            UpdateLastSyncText();
        }
        catch (Exception ex)
        {
            StatusText = NetworkErrors.Friendly(ex);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Register()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            StatusText = "Заполните email и пароль (≥6 символов).";
            return;
        }
        IsBusy = true;
        StatusText = "Регистрация...";
        using var coldStart = StartColdStartHint();
        try
        {
            await _auth.RegisterAsync(Email.Trim(), Password);
            IsLoggedIn = true;
            Password = "";
            StatusText = "Аккаунт создан.";
            UpdateLastSyncText();
        }
        catch (Exception ex)
        {
            StatusText = NetworkErrors.Friendly(ex);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _auth.LogoutAsync();
        // Сбрасываем курсор синхронизации — иначе следующий вход под другим
        // (или тем же) аккаунтом увидит stale LastSyncUtc и не отправит
        // локальные тренировки, которые «старее» этой метки.
        _sync.ResetLastSyncUtc();
        IsLoggedIn = false;
        Email = "";
        StatusText = "Вы вышли.";
        LastSyncText = "";
    }

    /// <summary>
    /// Принудительно сбрасывает LastSyncUtc и сразу запускает синхронизацию.
    /// Полезно когда курсор «застрял» (например, после неудачных попыток
    /// или из-за clock skew с сервером) и локальные тренировки не пушатся.
    /// </summary>
    [RelayCommand]
    private async Task ForceFullResync()
    {
        _sync.ResetLastSyncUtc();
        UpdateLastSyncText();
        await Sync();
    }

    [RelayCommand]
    private async Task Sync()
    {
        IsBusy = true;
        StatusText = "Синхронизация...";
        using var coldStart = StartColdStartHint();
        try
        {
            var stats = await _sync.RunOnceAsync();
            StatusText = $"Готово. Отправлено: {stats.Pushed}, новых: {stats.PulledNew}, обновлено: {stats.PulledUpdated}.\n{_sync.LastDiagnostics}";
            UpdateLastSyncText();
        }
        catch (Exception ex)
        {
            StatusText = NetworkErrors.Friendly(ex);
            // Если сервер сказал «токен невалиден» — AuthClient уже почистил
            // SecureStorage, переключаемся в форму входа.
            if (await _auth.GetTokenAsync() == null) IsLoggedIn = false;
        }
        finally { IsBusy = false; }
    }

    private void UpdateLastSyncText()
    {
        var last = _sync.LastSyncUtc;
        LastSyncText = last == null
            ? "Последняя синхронизация: ещё не было"
            : $"Последняя синхронизация: {last.Value.ToLocalTime():dd.MM.yyyy HH:mm}";
    }

    /// <summary>
    /// Через 5 сек после начала операции подменяет StatusText на подсказку
    /// про cold start. Render free tier засыпает через 15 минут и первый
    /// запрос после сна занимает ~30 сек. CTS отменяет таймер если операция
    /// закончилась раньше (в finally через using).
    /// </summary>
    private IDisposable StartColdStartHint()
    {
        var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                if (cts.IsCancellationRequested) return;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (IsBusy)
                        StatusText = "Сервер засыпал, поднимаем (~30 сек)...";
                });
            }
            catch (TaskCanceledException) { /* норм — операция завершилась */ }
        });
        return new ActionDisposable(() => cts.Cancel());
    }

    private sealed class ActionDisposable : IDisposable
    {
        private readonly Action _action;
        public ActionDisposable(Action action) => _action = action;
        public void Dispose() => _action();
    }
}
