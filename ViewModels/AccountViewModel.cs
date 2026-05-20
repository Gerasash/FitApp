using System.ComponentModel;
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
            StatusText = "Ошибка: " + ex.Message;
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
            StatusText = "Ошибка: " + ex.Message;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task Logout()
    {
        await _auth.LogoutAsync();
        IsLoggedIn = false;
        Email = "";
        StatusText = "Вы вышли.";
        LastSyncText = "";
    }

    [RelayCommand]
    private async Task Sync()
    {
        IsBusy = true;
        StatusText = "Синхронизация...";
        try
        {
            var stats = await _sync.RunOnceAsync();
            StatusText = $"Готово. Отправлено: {stats.Pushed}, новых: {stats.PulledNew}, обновлено: {stats.PulledUpdated}.";
            UpdateLastSyncText();
        }
        catch (Exception ex)
        {
            StatusText = "Ошибка: " + ex.Message;
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
}
