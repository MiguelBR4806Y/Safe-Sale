using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SS.Data;
using SS.Models;
using SS.Services;

namespace SS.ViewModels;

public partial class CashSessionViewModel : ViewModelBase
{
    private readonly CashSessionRepository _repo;
    private readonly int _userId;
    private readonly bool _isAdmin;

    [ObservableProperty]
    private CashSession? _currentSession;

    [ObservableProperty]
    private ObservableCollection<CashSession> _history = new();

    [ObservableProperty]
    private string _openingAmountText = "";

    [ObservableProperty]
    private string _closingAmountText = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string _statusColor = "#4CAF50";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isError;

    public bool HasOpenSession => CurrentSession != null;
    public bool IsClosed => CurrentSession == null;
    public bool IsAdmin => _isAdmin;

    public string SessionStateLabel => CurrentSession == null
        ? "Sin caja abierta"
        : $"Caja abierta desde {CurrentSession.OpenedAtLabel}";

    public string OpeningLabel => CurrentSession?.OpeningLabel ?? "$0.00";
    public string CashSalesLabel => CurrentSession?.CashSalesLabel ?? "$0.00";
    public string TotalSalesLabel => CurrentSession?.TotalSalesLabel ?? "$0.00";
    public string ExpectedCashLabel => CurrentSession?.ExpectedCashLabel ?? "$0.00";

    public ICommand OpenCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand RefreshCommand { get; }

    public CashSessionViewModel(string connectionString, User user)
    {
        _repo = new CashSessionRepository(connectionString);
        _userId = user.Id;
        _isAdmin = user.Role == "admin";

        OpenCommand = new RelayCommand(OnOpen);
        CloseCommand = new RelayCommand(OnClose);
        RefreshCommand = new RelayCommand(LoadData);

        LoadData();
    }

    public void LoadData()
    {
        CurrentSession = _repo.GetOpenSession(_userId);
        History = _repo.GetHistory(_userId, _isAdmin);

        IsError = false;
        ErrorMessage = "";
        StatusMessage = "";

        NotifyState();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasOpenSession));
        OnPropertyChanged(nameof(IsClosed));
        OnPropertyChanged(nameof(SessionStateLabel));
        OnPropertyChanged(nameof(OpeningLabel));
        OnPropertyChanged(nameof(CashSalesLabel));
        OnPropertyChanged(nameof(TotalSalesLabel));
        OnPropertyChanged(nameof(ExpectedCashLabel));
    }

    private void OnOpen()
    {
        if (CurrentSession != null) return;

        if (!InputValidator.TryParseMoney(OpeningAmountText, out var amount, out var error))
        {
            ShowError(error);
            return;
        }

        try
        {
            _repo.OpenSession(_userId, amount);
            OpeningAmountText = "";
            LoadData();
            ShowStatus("Caja abierta correctamente.", "#4CAF50");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OnClose()
    {
        if (CurrentSession == null) return;

        if (!InputValidator.TryParseMoney(ClosingAmountText, out var amount, out var error))
        {
            ShowError(error);
            return;
        }

        try
        {
            var closed = _repo.CloseSession(CurrentSession.Id, amount);
            ClosingAmountText = "";
            LoadData();

            var color = closed.Difference.HasValue && closed.Difference.Value < 0 ? "#EF5350" : "#4CAF50";
            ShowStatus($"Caja cerrada. Diferencia: {closed.DifferenceLabel}", color);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        IsError = true;
        ErrorMessage = message;
        StatusMessage = "";
    }

    private void ShowStatus(string message, string color)
    {
        IsError = false;
        ErrorMessage = "";
        StatusMessage = message;
        StatusColor = color;
    }
}
