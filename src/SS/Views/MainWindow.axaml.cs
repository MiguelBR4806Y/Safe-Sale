using System;
using Avalonia.Controls;
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
        Close();
    }
}
