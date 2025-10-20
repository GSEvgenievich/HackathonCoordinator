using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class VerifyEmailViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;
        private string _email;
        private string _code;

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        public RelayCommand VerifyCommand { get; }

        public VerifyEmailViewModel(string email)
        {
            _authService = new AuthService();
            _navigationService = App.NavigationService;
            _email = email;
            VerifyCommand = new RelayCommand(async () => await VerifyAsync());
        }

        private async Task VerifyAsync()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                MessageBox.Show("Введите код!");
                return;
            }

            var result = await _authService.VerifyCodeAsync(_email, Code);
            MessageBox.Show(result);

            if (result == "Ок")
                _navigationService.NavigateTo(new AuthorizationPage());
        }
    }
}
