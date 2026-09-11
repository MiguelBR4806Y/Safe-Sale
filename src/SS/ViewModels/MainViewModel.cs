using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SS.Models;

namespace SS.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _greeting = "Welcome to Safe-Sale!";

    [ObservableProperty]
    private string? _scannedCode;

    public ICommand ScanQrCommand { get; }

    public MainViewModel()
    {
        ScanQrCommand = new RelayCommand(async () => await ScanQrAsync());
    }

    private async Task ScanQrAsync()
    {
        // TODO: Integrar servicio QR cuando esté disponible
        // Para ya, mostraremos un mensaje indicando la funcionalidad
        ScannedCode = "Escaneado: (funcionalidad QR por implementar)";
    }
}