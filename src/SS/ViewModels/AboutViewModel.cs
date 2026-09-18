using System;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using SS.Services;

namespace SS.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _appVersion = "";

    [ObservableProperty]
    private string _appName = "Safe-Sale";

    [ObservableProperty]
    private string _description = "Sistema de Gestión de Supermercado";

    [ObservableProperty]
    private string _updateStatus = "";

    [ObservableProperty]
    private string _updateStatusColor = "#4A148C";

    [ObservableProperty]
    private bool _isChecking;

    public ICommand CheckForUpdatesCommand { get; }

    public AboutViewModel()
    {
        AppVersion = $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";
        CheckForUpdatesCommand = new RelayCommand(OnCheckForUpdates);
    }

    private async void OnCheckForUpdates()
    {
        IsChecking = true;
        UpdateStatus = "Buscando actualizaciones...";
        UpdateStatusColor = "#FF9800";

        try
        {
            UpdateService.Initialize();
            var hasUpdate = await UpdateService.CheckForUpdateAsync((msg) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    UpdateStatus = msg;
                });
            });

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (hasUpdate)
                {
                    UpdateStatus = "Actualización instalada. La app se reiniciará...";
                    UpdateStatusColor = "#4CAF50";
                }
                else
                {
                    UpdateStatus = "✓ Tu app está actualizada";
                    UpdateStatusColor = "#4CAF50";
                }
                IsChecking = false;
            });
        }
        catch (Exception ex)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateStatus = $"Error: {ex.Message}";
                UpdateStatusColor = "#EF5350";
                IsChecking = false;
            });
        }
    }
}
