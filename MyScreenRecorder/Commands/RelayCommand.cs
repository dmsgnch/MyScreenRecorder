using System;
using System.Windows.Input;

namespace MyScreenRecorder.Commands
{
    /// <summary>
    /// Universal command with synchronous execution
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<bool>? canExecute;

        public RelayCommand(Action<object> execute, Func<bool>? canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
        {
            execute.Invoke(parameter!);
        } 
        
        public event EventHandler? CanExecuteChanged;
        
        private void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseCanExecuteChanged() => OnCanExecuteChanged();
    }
}