using System;
using System.Windows.Input;

namespace Calcvik.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action? _execute;
    private readonly Action<object?>? _executeWithParameter;

    public RelayCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public RelayCommand(Action<object?> execute)
    {
        _executeWithParameter = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (_executeWithParameter is not null)
            _executeWithParameter(parameter);
        else
            _execute?.Invoke();
    }

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }
}
