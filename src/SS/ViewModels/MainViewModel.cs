using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using SS;
using SS.Views;

namespace SS.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    public ICommand NavigateToSalesViewCommand { get; }
    public ICommand NavigateToInventoryViewCommand { get; }
    public ICommand NavigateToRecordsViewCommand { get; }
    public ICommand NavigateToDashboardViewCommand { get; }

    public MainViewModel()
    {
        NavigateToSalesViewCommand = new RelayCommand(() => NavigateToView("sales"));
        NavigateToInventoryViewCommand = new RelayCommand(() => NavigateToView("inventory"));
        NavigateToRecordsViewCommand = new RelayCommand(() => NavigateToView("records"));
        NavigateToDashboardViewCommand = new RelayCommand(() => NavigateToView("dashboard"));

        NavigateToView("dashboard");
    }

    private void NavigateToView(string viewName)
    {
        switch (viewName)
        {
            case "sales":
                CurrentView = new SalesView();
                break;
            case "inventory":
                CurrentView = new InventoryView();
                break;
            case "records":
                CurrentView = new RecordsView();
                break;
            case "dashboard":
                CurrentView = new DashboardView();
                break;
        }
    }
}