using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SS.Data;
using SS.Models;
using SS.ViewModels;
using SS.Views;

namespace SS;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ShowLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var loginWindow = new LoginWindow();
        var loginViewModel = new LoginViewModel(AppDatabase.DbPath);
        loginWindow.DataContext = loginViewModel;

        loginViewModel.LoginSuccess += (user) =>
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(user)
            };
            mainWindow.Show();
            loginWindow.Close();
            desktop.MainWindow = mainWindow;
        };

        loginWindow.Closed += (sender, args) =>
        {
            if (desktop.MainWindow == null)
            {
                desktop.Shutdown();
            }
        };

        loginWindow.Show();
    }
}