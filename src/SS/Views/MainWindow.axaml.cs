using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SS.Data;
using SS.ViewModels;

namespace SS.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var mainViewModel = DataContext as MainViewModel;
        if (mainViewModel != null)
        {
            mainViewModel.LogoutRequested += OnLogout;
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
