using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class AuthorizationViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly AuthService _authService;
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

        public RelayCommand LoginCommand { get; }
        public RelayCommand NavigateToRegistrationCommand { get; }

        public AuthorizationViewModel()
        {
            _navigationService = App.NavigationService;
            _authService = new AuthService();

            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync());
            NavigateToRegistrationCommand = new RelayCommand(() =>
                _navigationService.NavigateTo(new RegistrationPage()));
        }

        private async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            var resultMessage = await _authService.LoginAsync(Login, Password);

            if (resultMessage == "OK")
            {
                MessageBox.Show("Успешный вход!");
                _navigationService.NavigateTo(new NoTeamPage());
            }
            else
            {
                MessageBox.Show(resultMessage);
            }
        }
    }
}
