using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyScreenRecorder.Commands
{
    /// <summary>
    /// Universal command with asynchronous execution
    /// </summary>
    public class RelayCommandAsync : ICommand
    {
        private readonly Func<object, Task> execute;
        private readonly Func<bool> canExecute;
    
        public RelayCommandAsync(Func<object, Task> execute, Func<bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }
    
        public event EventHandler CanExecuteChanged;
        
        private bool isExecuting;
        private bool IsExecuting
        {
            get => isExecuting;
            set
            {
                if (!value.Equals(isExecuting))
                {
                    isExecuting = value;
                    OnCanExecuteChanged();
                }
            }
        }
    
        public bool CanExecute(object parameter) => (canExecute?.Invoke() ?? true) && !IsExecuting;
    
        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter);
        } 
        public async Task ExecuteAsync(object parameter)
        {
            if (!CanExecute(null)) throw new Exception("Command cant be executed!");
            
            IsExecuting = true;
            
            try
            {
                await execute.Invoke(parameter);
            }
            catch (Exception ex)
            {
                throw new Exception($"Operation Error: {ex.Message}");
            }
            finally
            {
                IsExecuting = false;
            }
        }
    
        private async void OnCanExecuteChanged()
        {
            var dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
            if (dispatcher != null)
            {
                if (dispatcher.HasThreadAccess)
                {
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    });
                }
            }
        }
    
        public void RaiseCanExecuteChanged() => OnCanExecuteChanged();
    }
}