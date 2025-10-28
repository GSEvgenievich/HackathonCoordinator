using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.Storages;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class GitHubAuthViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly GitHubOAuthService _gitHubService;
        private readonly UserService _userService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string _authorizationCode;
        public string AuthorizationCode
        {
            get => _authorizationCode;
            set { _authorizationCode = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); }
        }

        public bool IsNotLoading => !IsLoading;

        private bool _showCodeInput;
        public bool ShowCodeInput
        {
            get => _showCodeInput;
            set { _showCodeInput = value; OnPropertyChanged(); }
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set { _isSuccess = value; OnPropertyChanged(); }
        }

        public ICommand StartOAuthCommand { get; }
        public ICommand ConfirmCodeCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoToProfileCommand { get; }

        public GitHubAuthViewModel()
        {
            _navigationService = App.NavigationService;
            _gitHubService = new GitHubOAuthService();
            _userService = new UserService();

            StartOAuthCommand = new RelayCommand(async () => await StartOAuthFlowAsync());
            ConfirmCodeCommand = new RelayCommand(async () => await ConfirmAuthorizationCodeAsync());
            GoBackCommand = new RelayCommand(() => _navigationService.NavigateTo(new ProfilePage()));
            GoToProfileCommand = new RelayCommand(() => _navigationService.NavigateTo(new ProfilePage()));

            StatusMessage = "Нажмите 'Начать привязку', чтобы запустить процесс OAuth авторизации GitHub.";
        }

        private async Task StartOAuthFlowAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Получение ссылки авторизации...";

                var state = Guid.NewGuid().ToString();
                SecureTokenStorage.SaveTempState(state);

                var authUrl = await _gitHubService.GetGitHubAuthUrlAsync(state);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                StatusMessage = "Браузер открыт. После авторизации GitHub перенаправит вас на страницу с кодом. Скопируйте код и вставьте его в поле ниже.";
                ShowCodeInput = true;

            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ConfirmAuthorizationCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(AuthorizationCode))
            {
                MessageBox.Show("Введите код авторизации");
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Обмен кода на токен...";

                var savedState = SecureTokenStorage.GetTempState();

                var result = await _gitHubService.ExchangeCodeAsync(AuthorizationCode);

                StatusMessage = "Привязка аккаунта к вашему профилю...";

                var linkResult = await _userService.LinkGitHubAccountAsync(
                    result.AccessToken,
                    result.UserInfo.Login,
                    result.UserInfo.AvatarUrl);

                if (linkResult.IsSuccess)
                {
                    StatusMessage = "GitHub аккаунт успешно привязан!";
                    IsSuccess = true;
                }
                else
                {
                    StatusMessage = $"Ошибка привязки: {linkResult.Message}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                SecureTokenStorage.ClearTempState();
            }
        }
    }
}