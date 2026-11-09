using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SS.ViewModels;

namespace SS.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel? _viewModel;

    public LoginWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        RegisterLink.Click += OnRegisterClick;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            _viewModel = vm;
        }
    }

    private async void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        var dialog = new RegisterWindow(SS.Data.DatabaseConfig.ConnectionString);
        var result = await dialog.ShowDialog<bool?>(this);

        if (result == true)
        {
            _viewModel.Username = dialog.RegisteredUsername ?? "";
            _viewModel.Password = "";
            _viewModel.InfoMessage = "Usuario creado correctamente. Iniciá sesión.";
            _viewModel.IsInfoVisible = true;
            PasswordBox.Focus();
        }
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel != null && !_viewModel.IsLoading)
        {
            _viewModel.LoginCommand.Execute(null);
        }
    }
}
