using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SS.Dialogs;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;

        ConfirmButton.Click += (_, _) => Close(true);
        CancelButton.Click += (_, _) => Close(false);
    }
}
