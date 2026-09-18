using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using SS.Data;
using SS.Models;
using SS.Services;

namespace SS.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly SqliteUserRepository _userRepo;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private string _passwordChar = "*";

    [ObservableProperty]
    private string _eyeIcon = "\U0001F441";

    [ObservableProperty]
    private string _updateMessage = "";

    [ObservableProperty]
    private bool _isUpdateAvailable;

    public ICommand LoginCommand { get; }
    public ICommand TogglePasswordCommand { get; }

    public event Action<User>? LoginSuccess;

    public LoginViewModel(string dbPath)
    {
        _userRepo = new SqliteUserRepository(dbPath);
        LoginCommand = new RelayCommand(OnLogin);
        TogglePasswordCommand = new RelayCommand(OnTogglePassword);

        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            UpdateService.Initialize();
            var hasUpdate = await UpdateService.CheckForUpdateAsync((msg) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    UpdateMessage = msg;
                });
            });

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (hasUpdate)
                {
                    IsUpdateAvailable = true;
                    UpdateMessage = "Actualización instalada. La app se reiniciará...";
                }
                else
                {
                    UpdateMessage = "";
                }
            });
        }
        catch
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateMessage = "";
            });
        }
    }

    partial void OnUsernameChanged(string value)
    {
        ClearError();
    }

    partial void OnPasswordChanged(string value)
    {
        ClearError();
    }

    private void ClearError()
    {
        if (IsError)
        {
            IsError = false;
            ErrorMessage = "";
        }
    }

    private void OnTogglePassword()
    {
        IsPasswordVisible = !IsPasswordVisible;
        PasswordChar = IsPasswordVisible ? "" : "*";
        EyeIcon = IsPasswordVisible ? "\U0001F441\U0001F441" : "\U0001F441";
    }

    private void OnLogin()
    {
        IsError = false;
        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Ingrese su usuario";
            IsError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Ingrese su contraseña";
            IsError = true;
            return;
        }

        IsLoading = true;

        var user = _userRepo.GetByUsername(Username);

        if (user == null)
        {
            ErrorMessage = "Usuario no encontrado";
            IsError = true;
            IsLoading = false;
            return;
        }

        if (!PasswordHasher.VerifyPassword(Password, user.PasswordHash))
        {
            ErrorMessage = "Contraseña incorrecta";
            IsError = true;
            IsLoading = false;
            return;
        }

        IsLoading = false;
        LoginSuccess?.Invoke(user);
    }

    public void Reset()
    {
        Username = "";
        Password = "";
        ErrorMessage = "";
        IsError = false;
        IsLoading = false;
        IsPasswordVisible = false;
        PasswordChar = "*";
        EyeIcon = "\U0001F441";
    }
}
