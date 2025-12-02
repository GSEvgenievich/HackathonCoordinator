using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.Helpers
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private readonly Action<Exception> _onException;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isExecuting;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool IsExecuting => _isExecuting;
        public bool CanCancel => _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;

        public AsyncRelayCommand(
            Func<Task> execute,
            Func<bool> canExecute = null,
            Action<Exception> onException = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onException = onException;
        }

        public AsyncRelayCommand(
            Func<CancellationToken, Task> execute,
            Func<bool> canExecute = null,
            Action<Exception> onException = null)
        {
            _execute = () =>
            {
                _cancellationTokenSource = new CancellationTokenSource();
                return execute(_cancellationTokenSource.Token);
            };
            _canExecute = canExecute;
            _onException = onException;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object parameter)
        {
            await ExecuteAsync();
        }

        public async Task ExecuteAsync()
        {
            if (!CanExecute(null))
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute();
            }
            catch (OperationCanceledException)
            {
                // Операция была отменена - это нормально
                System.Diagnostics.Debug.WriteLine("Операция отменена");
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                _isExecuting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                RaiseCanExecuteChanged();
            }
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void HandleException(Exception ex)
        {
            if (_onException != null)
            {
                _onException(ex);
            }
            else
            {
                // Стандартная обработка исключения
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Произошла ошибка: {ex.Message}\n\nПроверьте подключение к серверу и повторите попытку.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private readonly Predicate<T> _canExecute;
        private readonly Action<Exception> _onException;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isExecuting;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool IsExecuting => _isExecuting;
        public bool CanCancel => _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;

        public AsyncRelayCommand(
            Func<T, Task> execute,
            Predicate<T> canExecute = null,
            Action<Exception> onException = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _onException = onException;
        }

        public AsyncRelayCommand(
            Func<T, CancellationToken, Task> execute,
            Predicate<T> canExecute = null,
            Action<Exception> onException = null)
        {
            _execute = (parameter) =>
            {
                _cancellationTokenSource = new CancellationTokenSource();
                return execute(parameter, _cancellationTokenSource.Token);
            };
            _canExecute = canExecute;
            _onException = onException;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke((T)parameter) ?? true);
        }

        public async void Execute(object parameter)
        {
            await ExecuteAsync((T)parameter);
        }

        public async Task ExecuteAsync(T parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute(parameter);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Операция отменена");
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                _isExecuting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                RaiseCanExecuteChanged();
            }
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void HandleException(Exception ex)
        {
            if (_onException != null)
            {
                _onException(ex);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Произошла ошибка: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}