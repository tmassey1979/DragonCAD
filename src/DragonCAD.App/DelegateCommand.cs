using System.Windows.Input;

namespace DragonCAD.App;

public sealed class DelegateCommand : ICommand
{
    private readonly Action<object?> execute;
    private readonly Func<bool>? canExecute;

    public DelegateCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute)
    {
    }

    public DelegateCommand(Action<object?> execute, Func<bool>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => execute(parameter);

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
