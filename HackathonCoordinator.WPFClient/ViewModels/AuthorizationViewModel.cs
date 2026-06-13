using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class AuthorizationViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly UserService _userService;

        private string _login = "";
        private string _password = "";
        private bool _isLoggingIn;

        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand NavigateToRegistrationCommand { get; }

        public AuthorizationViewModel()
        {
            _authService = new AuthService();
            _userService = new UserService();

            LoginCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteLoginAsync(),
                canExecute: () => !IsLoggingIn);

            NavigateToRegistrationCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteNavigateToRegistrationAsync(),
                canExecute: () => !IsLoggingIn);
        }

        private async Task ExecuteLoginAsync()
        {
            if (IsLoggingIn) return;

            IsLoggingIn = true;

            try
            {
                var resultMessage = await _authService.LoginAsync(Login, Password);

                if (!resultMessage.Success)
                {
                    await ShowErrorAsync(resultMessage.Message);
                    return;
                }

                var user = await _userService.GetCurrentUserAsync();
                if (!user.Success)
                {
                    await ShowErrorAsync("Не удалось загрузить данные пользователя");
                    return;
                }

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (Application.Current.MainWindow is MainWindow mainWindow &&
                        mainWindow.DataContext is MainWindowViewModel mainViewModel)
                    {
                        mainViewModel.Username = user.Data.Username;
                        mainViewModel.CheckUserRole();
                        mainViewModel.GetUsername();
                        mainViewModel.InitializeNotificationsSignalR();
                        await mainViewModel.OpenMainPage();
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка входа: {ex.Message}");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private async Task ExecuteNavigateToRegistrationAsync()
        {
            if (IsLoggingIn) return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _navigationService.NavigateTo(new RegistrationPage());
            });
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            if (_authService is IDisposable authDisposable) authDisposable.Dispose();
            if (_userService is IDisposable userDisposable) userDisposable.Dispose();
        }
    }
}