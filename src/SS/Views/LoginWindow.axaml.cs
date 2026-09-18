using Avalonia.Controls;
using Avalonia.Input;
using SS.ViewModels;

namespace SS.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel? _viewModel;

    public LoginWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            _viewModel = vm;
        }
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel != null && !_viewModel.IsLoading)
        {
            _viewModel.LoginCommand.Execute(null);
        }
    }
}
