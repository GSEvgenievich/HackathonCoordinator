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
        private readonly NavigationService _navigationService;
        private readonly AuthService _authService;
        private readonly UserService _userService;

        private string _login = "";
        private string _password = "";

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

        // Только асинхронные команды
        public ICommand LoginCommand { get; }
        public ICommand NavigateToRegistrationCommand { get; }

        public AuthorizationViewModel()
        {
            _navigationService = App.NavigationService;
            _authService = new AuthService();
            _userService = new UserService();

            // AsyncRelayCommand для асинхронного входа
            LoginCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteLoginAsync(),
                canExecute: () => !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password));

            // Простая навигация - можно оставить RelayCommand
            NavigateToRegistrationCommand = new RelayCommand(
                () => _navigationService.NavigateTo(new RegistrationPage()));
        }

        private async Task ExecuteLoginAsync()
        {
            try
            {
                var resultMessage = await _authService.LoginAsync(Login, Password);

                if (resultMessage.Success)
                {
                    var user = await _userService.GetCurrentUserAsync();

                    // Обновление главного окна
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                            {
                                mainViewModel.Username = user.Data.Username;
                                mainViewModel.CheckUserRole();
                                mainViewModel.GetUsername();
                                mainViewModel.InitializeNotificationsSignalR();
                            }
                        }
                    });

                    // Навигация
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (user.Data.TeamId == null)
                            _navigationService.NavigateTo(new CompetitionsPage());
                        else
                            _navigationService.NavigateTo(new TeamPage());
                    });
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(resultMessage.Message, "Ошибка входа",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка входа: {ex.Message}\n\nПроверьте подключение к серверу.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            if (_authService is IDisposable authDisposable)
                authDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();
        }
    }
}