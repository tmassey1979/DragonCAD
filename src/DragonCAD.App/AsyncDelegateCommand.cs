using System.Windows.Input;

namespace DragonCAD.App;

public sealed class AsyncDelegateCommand : ICommand
{
    private readonly Func<Task> execute;
    private readonly Func<bool>? canExecute;

    public AsyncDelegateCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter) => await execute();

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
