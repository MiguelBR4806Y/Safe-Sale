using System;
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

    public ICommand LoginCommand { get; }

    public event Action<User>? LoginSuccess;

    public LoginViewModel(string dbPath)
    {
        _userRepo = new SqliteUserRepository(dbPath);
        LoginCommand = new RelayCommand(OnLogin);
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
    }
}
