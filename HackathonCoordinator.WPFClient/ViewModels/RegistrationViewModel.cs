using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;

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
            set => SetProperty(ref _username, value);
        }

        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public RelayCommand RegisterCommand { get; }
        public RelayCommand NavigateToAuthorizationCommand { get; }

        public RegistrationViewModel()
        {
            _navigationService = App.NavigationService;
            _authService = new AuthService();

            RegisterCommand = new RelayCommand(async () => await ExecuteRegisterAsync());
            NavigateToAuthorizationCommand = new RelayCommand(() =>
                _navigationService.NavigateTo(new AuthorizationPage()));
        }

        private async Task ExecuteRegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Login) ||
            string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Пожалуйста, заполните все поля!");
                return;
            }

            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!");
                return;
            }

            var registration = await _authService.RegisterAsync(Username, Login, Email, Password);
            MessageBox.Show(registration.Message);

            if (registration.Success)
                _navigationService.NavigateTo(new AuthorizationPage());
        }
    }
}