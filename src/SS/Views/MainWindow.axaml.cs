using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SS.Data;
using SS.ViewModels;

namespace SS.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _subscribedViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.LogoutRequested -= OnLogout;
        }

        if (DataContext is MainViewModel mainViewModel)
        {
            _subscribedViewModel = mainViewModel;
            _subscribedViewModel.LogoutRequested += OnLogout;
        }
    }

    private void OnLogout()
    {
        var loginWindow = new LoginWindow();
        var loginViewModel = new LoginViewModel(AppDatabase.DbPath);
        loginWindow.DataContext = loginViewModel;

        loginViewModel.LoginSuccess += (user) =>
        {
            var newMainWindow = new MainWindow
            {
                DataContext = new MainViewModel(user)
            };
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = newMainWindow;
            }
            newMainWindow.Show();
            this.Close();
        };

        loginWindow.Closed += (sender, args) =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow == null)
                {
                    desktop.Shutdown();
                }
            }
        };

        loginWindow.Show();
        this.Hide();
    }
}
