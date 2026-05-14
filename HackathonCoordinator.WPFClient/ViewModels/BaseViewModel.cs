using HackathonCoordinator.WPFClient.Services;
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

        protected static async Task ShowErrorAsync(string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
