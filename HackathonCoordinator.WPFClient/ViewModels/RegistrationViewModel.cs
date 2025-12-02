using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class RegistrationViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly AuthService _authService;

        private string _username = "";
        private string _login = "";
        private string _email = "";
        private string _password = "";
        private string _confirmPassword = "";

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                ((AsyncRelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public string Login
        {
            get => _login;
            set
            {
                SetProperty(ref _login, value);
                ((AsyncRelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                SetProperty(ref _email, value);
                ((AsyncRelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                ((AsyncRelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        // Заменяем на AsyncRelayCommand
        public ICommand RegisterCommand { get; }
        public ICommand NavigateToAuthorizationCommand { get; }

        public RegistrationViewModel()
        {
            _navigationService = App.NavigationService;
            _authService = new AuthService();

            // AsyncRelayCommand для безопасной регистрации
            RegisterCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteRegisterAsync(),
                canExecute: () => !string.IsNullOrWhiteSpace(Username) &&
                                 !string.IsNullOrWhiteSpace(Login) &&
                                 !string.IsNullOrWhiteSpace(Email) &&
                                 !string.IsNullOrWhiteSpace(Password) &&
                                 !string.IsNullOrWhiteSpace(ConfirmPassword));

            NavigateToAuthorizationCommand = new RelayCommand(
                () => _navigationService.NavigateTo(new AuthorizationPage()));
        }

        private async Task ExecuteRegisterAsync()
        {
            if (Password != ConfirmPassword)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("Пароли не совпадают!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            try
            {
                var registration = await _authService.RegisterAsync(Username, Login, Email, Password);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(registration.Message,
                        registration.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        registration.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (registration.Success)
                        _navigationService.NavigateTo(new AuthorizationPage());
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка регистрации: {ex.Message}\n\nПроверьте подключение к серверу.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            if (_authService is IDisposable authDisposable)
                authDisposable.Dispose();
        }
    }
}