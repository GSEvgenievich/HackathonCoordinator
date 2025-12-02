using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _disposed = false;

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
