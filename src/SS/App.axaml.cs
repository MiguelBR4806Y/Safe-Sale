using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SS.Data;
using SS.Models;
using SS.ViewModels;
using SS.Views;

namespace SS;

public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private LoginWindow? _currentLoginWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ShowLoginWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowLoginWindow()
    {
        if (_desktop == null) return;

        _currentLoginWindow = new LoginWindow();
        var loginViewModel = new LoginViewModel(AppDatabase.DbPath);
        _currentLoginWindow.DataContext = loginViewModel;

        loginViewModel.LoginSuccess += (user) =>
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(user)
            };

            mainWindow.Closed += MainWindow_Closed;

            _desktop.MainWindow = mainWindow;
            mainWindow.Show();
            _currentLoginWindow?.Close();
            _currentLoginWindow = null;
        };

        _currentLoginWindow.Show();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (_desktop == null) return;

        _desktop.MainWindow = null;
        ShowLoginWindow();
    }
}