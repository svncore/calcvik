using Avalonia.Controls;
using Avalonia.Input;
using Calcvik.ViewModels;

namespace Calcvik.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new CalculatorViewModel();
        Opened += (_, _) => Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (FocusManager?.GetFocusedElement() is TextBox && e.Key is not (Key.Enter or Key.Escape))
        {
            base.OnKeyDown(e);
            return;
        }

        if (DataContext is CalculatorViewModel viewModel && viewModel.HandleKey(e.Key))
        {
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
