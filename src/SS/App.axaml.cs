using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using SS.Data;
using SS.Models;
using SS.Services;
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
            desktop.Exit += (_, _) => MySqlServerManager.Instance.Stop();

            StartDatabaseAndShowApp();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void StartDatabaseAndShowApp()
    {
        var status = new TextBlock
        {
            Text = "Iniciando base de datos local...",
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#4A148C")),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var hint = new TextBlock
        {
            Text = "La primera vez puede tardar unos segundos.",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#6C3483")),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var progress = new ProgressBar { IsIndeterminate = true, Width = 320, Height = 6 };

        var stack = new StackPanel
        {
            Margin = new Thickness(28),
            Spacing = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { status, progress, hint }
        };

        var splash = new Window
        {
            Title = "Safe-Sale",
            Width = 440,
            Height = 190,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = stack
        };

        splash.Show();

        Task.Run(() =>
        {
            try
            {
                MySqlServerManager.Instance.Start();
                DatabaseInitializer.Initialize(DatabaseConfig.ConnectionString, DatabaseConfig.ServerAdminConnectionString);
                Dispatcher.UIThread.Post(() =>
                {
                    splash.Close();
                    ShowLoginWindow();
                });
            }
            catch (Exception ex)
            {
                LogStartupError(ex);
                Dispatcher.UIThread.Post(() =>
                {
                    splash.Close();
                    StartupState.Error = ex;
                    ShowStartupErrorWindow(ex);
                });
            }
        });
    }

    private void ShowLoginWindow()
    {
        if (_desktop == null) return;

        _currentLoginWindow = new LoginWindow();
        var loginViewModel = new LoginViewModel(DatabaseConfig.ConnectionString);
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

    private void ShowStartupErrorWindow(Exception error)
    {
        if (_desktop == null) return;

        var messageBlock = new TextBlock
        {
            Text = error.Message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 640,
            Foreground = new SolidColorBrush(Color.Parse("#4A148C"))
        };

        var titleBlock = new TextBlock
        {
            Text = "No se pudo iniciar la base de datos local",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#4A148C")),
            TextWrapping = TextWrapping.Wrap
        };

        var hintBlock = new TextBlock
        {
            Text = "El motor MySQL local se inicia y configura solo. Si el problema persiste, " +
                   "revisá el log o reinstalá la aplicación.",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#6C3483")),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 640
        };

        var retryButton = new Button { Content = "Reintentar", Padding = new Thickness(16, 8) };
        var logsButton = new Button { Content = "Abrir carpeta de logs", Padding = new Thickness(16, 8) };
        var exitButton = new Button { Content = "Salir", Padding = new Thickness(16, 8) };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { retryButton, logsButton, exitButton }
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(28),
            Spacing = 14,
            Children = { titleBlock, messageBlock, hintBlock, buttons }
        };

        var window = new Window
        {
            Title = "Safe-Sale",
            Width = 720,
            Height = 380,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://SafeSale/Assets/SS-ico.png"))),
            Content = new ScrollViewer { Content = panel }
        };

        retryButton.Click += async (_, _) =>
        {
            retryButton.IsEnabled = false;
            messageBlock.Text = "Reintentando...";

            try
            {
                await Task.Run(() =>
                {
                    MySqlServerManager.Instance.Start();
                    DatabaseInitializer.Initialize(DatabaseConfig.ConnectionString, DatabaseConfig.ServerAdminConnectionString);
                });

                StartupState.Error = null;
                window.Close();
                ShowLoginWindow();
            }
            catch (Exception ex)
            {
                LogStartupError(ex);
                StartupState.Error = ex;
                messageBlock.Text = ex.Message;
                retryButton.IsEnabled = true;
            }
        };

        logsButton.Click += (_, _) =>
        {
            try
            {
                AppPaths.EnsureCreated();
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppPaths.LogDirectory,
                    UseShellExecute = true
                });
            }
            catch { }
        };

        exitButton.Click += (_, _) => _desktop.Shutdown();

        window.Show();
    }

    private static void LogStartupError(Exception ex)
    {
        try
        {
            AppPaths.EnsureCreated();
            File.AppendAllText(
                AppPaths.StartupLogFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR DE ARRANQUE:{Environment.NewLine}{ex}{Environment.NewLine}");
        }
        catch
        {
            // La ventana de error muestra el mensaje igualmente.
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (_desktop == null) return;

        _desktop.MainWindow = null;
        ShowLoginWindow();
    }
}
