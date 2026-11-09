using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SS.Data;
using SS.Models;
using SS.Services;

namespace SS.Views;

public partial class RegisterWindow : Window
{
    private readonly UserRepository _userRepo;

    public string? RegisteredUsername { get; private set; }

    public RegisterWindow() : this(DatabaseConfig.ConnectionString) { }

    public RegisterWindow(string connectionString)
    {
        InitializeComponent();

        _userRepo = new UserRepository(connectionString);

        UsernameBox.TextChanged += (_, _) => HideErrors();
        PasswordBox.TextChanged += (_, _) => HideErrors();
        ConfirmBox.TextChanged += (_, _) => HideErrors();

        RegisterButton.Click += OnRegister;
        CancelButton.Click += (_, _) => Close(false);
    }

    private void OnRegister(object? sender, RoutedEventArgs e)
    {
        if (!UserValidator.TryValidateUsername(UsernameBox.Text, out var username, out var usernameError))
        {
            ShowFieldError(UsernameError, UsernameBox, usernameError);
            return;
        }

        if (!UserValidator.TryValidatePassword(PasswordBox.Text, ConfirmBox.Text, out var passwordError))
        {
            if (passwordError.Contains("coinciden", StringComparison.OrdinalIgnoreCase))
                ShowFieldError(ConfirmError, ConfirmBox, passwordError);
            else
                ShowFieldError(PasswordError, PasswordBox, passwordError);
            return;
        }

        var password = PasswordBox.Text ?? string.Empty;

        try
        {
            if (_userRepo.GetByUsername(username) != null)
            {
                ShowFieldError(UsernameError, UsernameBox, "Ya existe un usuario con ese nombre.");
                return;
            }

            _userRepo.Add(new User
            {
                Username = username,
                PasswordHash = PasswordHasher.HashPassword(password),
                Role = "cashier"
            });

            RegisteredUsername = username;
            Close(true);
        }
        catch (Exception ex) when (DbErrors.IsDuplicateKey(ex))
        {
            ShowFieldError(UsernameError, UsernameBox, "Ya existe un usuario con ese nombre.");
        }
        catch (Exception)
        {
            ShowGlobalError("No se pudo crear el usuario. Intentá de nuevo.");
        }
    }

    private void ShowFieldError(TextBlock errorBlock, TextBox box, string message)
    {
        HideErrors();
        errorBlock.Text = message;
        errorBlock.IsVisible = true;
        box.BorderBrush = new SolidColorBrush(Color.Parse("#EF5350"));
    }

    private void ShowGlobalError(string message)
    {
        HideErrors();
        ErrorText.Text = message;
        ErrorBorder.IsVisible = true;
    }

    private void HideErrors()
    {
        UsernameError.IsVisible = false;
        PasswordError.IsVisible = false;
        ConfirmError.IsVisible = false;
        ErrorBorder.IsVisible = false;

        var normal = new SolidColorBrush(Color.Parse("#E8DAEF"));
        UsernameBox.BorderBrush = normal;
        PasswordBox.BorderBrush = normal;
        ConfirmBox.BorderBrush = normal;
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnRegister(this, new RoutedEventArgs());
        else if (e.Key == Key.Escape)
            Close(false);
    }
}
