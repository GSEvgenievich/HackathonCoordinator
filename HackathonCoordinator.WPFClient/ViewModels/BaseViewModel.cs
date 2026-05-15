using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.UserControls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected readonly NavigationService _navigationService = App.NavigationService;

        private bool _disposed = false;
        private bool _isLoading = false;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected static async Task<bool> ShowConfirmationAsync(string message, string title = "Подтверждение")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return DialogWindow.ShowConfirmation(message, title);
            });
        }

        protected static async Task<bool?> ShowYesNoCancelAsync(string message, string title = "Подтверждение")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return DialogWindow.ShowYesNoCancel(message, title);
            });
        }

        protected async Task<string?> ShowInputDialogAsync(string title, string prompt, string defaultValue = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new InputDialogWindow(title, prompt, defaultValue);
                dialog.Owner = Application.Current.MainWindow;
                return dialog.ShowDialog() == true ? dialog.InputValue : null;
            });
        }

        protected static async Task ShowInfoAsync(string message, string title = "Информация")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DialogWindow.Show(message, title, DialogType.Info);
            });
        }

        protected static async Task ShowSuccessAsync(string message, string title = "Успешно")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DialogWindow.Show(message, title, DialogType.Success);
            });
        }

        protected static async Task ShowWarningAsync(string message, string title = "Предупреждение")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DialogWindow.Show(message, title, DialogType.Warning);
            });
        }

        protected static async Task ShowErrorAsync(string message, string title = "Ошибка")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DialogWindow.Show(message, title, DialogType.Error);
            });
        }

        protected void Back()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _navigationService.GoBack();
            });
        }

        // Метод для очистки управляемых ресурсов (переопределяется в наследниках)
        protected virtual void DisposeManagedResources()
        {
        }

        // Метод для очистки неуправляемых ресурсов (переопределяется в наследниках)
        protected virtual void DisposeUnmanagedResources()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Очистка управляемых ресурсов
                    DisposeManagedResources();
                }

                // Очистка неуправляемых ресурсов
                DisposeUnmanagedResources();

                _disposed = true;
            }
        }

        ~BaseViewModel()
        {
            Dispose(false);
        }
    }
}
