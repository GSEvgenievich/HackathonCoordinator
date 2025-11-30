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

        private string _currentGitHubAccount;
        public string CurrentGitHubAccount
        {
            get => _currentGitHubAccount;
            set { _currentGitHubAccount = value; OnPropertyChanged(); }
        }

        public ICommand StartOAuthCommand { get; }
        public ICommand ConfirmCodeCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoToProfileCommand { get; }
        public ICommand ChangeAccountCommand { get; }

        public GitHubAuthViewModel()
        {
            _navigationService = App.NavigationService;
            _gitHubService = new GitHubOAuthService();
            _userService = new UserService();

            StartOAuthCommand = new RelayCommand(async () => await StartOAuthFlowAsync());
            ConfirmCodeCommand = new RelayCommand(async () => await ConfirmAuthorizationCodeAsync());
            GoToProfileCommand = new RelayCommand(() => _navigationService.NavigateTo(new ProfilePage()));
            ChangeAccountCommand = new RelayCommand(async () => await ChangeGitHubAccountAsync());

            LoadCurrentGitHubInfo();
        }

        private async void LoadCurrentGitHubInfo()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (!string.IsNullOrEmpty(user.Data.GitHubUsername))
                {
                    CurrentGitHubAccount = user.Data.GitHubUsername;
                    StatusMessage = $"✅ Ваш аккаунт GitHub уже привязан: {user.Data.GitHubUsername}";
                    ShowCodeInput = false;
                }
                else
                {
                    StatusMessage = "Нажмите 'Начать привязку', чтобы подключить GitHub аккаунт к вашему профилю.";
                    ShowCodeInput = false;
                }
            }
            catch
            {
                StatusMessage = "Нажмите 'Начать привязку', чтобы подключить GitHub аккаунт к вашему профилю.";
            }
        }

        private async Task StartOAuthFlowAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "🔗 Подготовка OAuth авторизации GitHub...";

                var state = Guid.NewGuid().ToString();
                SecureTokenStorage.SaveTempState(state);

                var authUrl = await _gitHubService.GetGitHubAuthUrlAsync(state);

                // Открываем браузер с инструкциями
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = authUrl.Data.AuthUrl,
                    UseShellExecute = true
                });

                StatusMessage = @"🌐 Браузер открыт! 

📋 Инструкция:
1. Войдите в нужный аккаунт GitHub (или создайте новый)
2. Предоставьте права доступа приложению
3. Скопируйте код авторизации со страницы подтверждения
4. Вставьте код в поле ниже

💡 Совет: Если хотите привязать другой аккаунт, просто войдите в него в браузере";

                ShowCodeInput = true;
                IsSuccess = false;

            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Ошибка: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ChangeGitHubAccountAsync()
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите отвязать текущий GitHub аккаунт и привязать новый?",
                "Смена GitHub аккаунта",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var unlinkResult = await _userService.UnlinkGitHubAsync();

                    if (unlinkResult.Success)
                    {
                        CurrentGitHubAccount = null;
                        StatusMessage = "✅ Текущий GitHub аккаунт отвязан. Теперь вы можете привязать новый аккаунт.";
                        ShowCodeInput = false;
                        await StartOAuthFlowAsync();
                    }
                    else
                    {
                        StatusMessage = $"❌ Ошибка отвязки: {unlinkResult.Message}";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Ошибка: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task ConfirmAuthorizationCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(AuthorizationCode))
            {
                MessageBox.Show("Введите код авторизации из браузера");
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "🔄 Обмен кода на токен доступа...";

                var result = await _gitHubService.ExchangeCodeAsync(AuthorizationCode);

                StatusMessage = "🔗 Привязка аккаунта к вашему профилю...";

                var linkResult = await _userService.LinkGitHubAccountAsync(
                    result.Data.AccessToken,
                    result.Data.UserInfo.Login,
                    result.Data.UserInfo.AvatarUrl);

                if (linkResult.Success)
                {
                    StatusMessage = $"✅ GitHub аккаунт успешно привязан!\n\n👤 Имя пользователя: {result.Data.UserInfo.Login}\n📧 Email: {result.Data.UserInfo.Email ?? "Не указан"}";
                    CurrentGitHubAccount = result.Data.UserInfo.Login;
                    IsSuccess = true;
                    ShowCodeInput = false;

                    // Очищаем поле ввода
                    AuthorizationCode = string.Empty;
                }
                else
                {
                    StatusMessage = $"❌ Ошибка привязки: {linkResult.Message}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Ошибка: {ex.Message}\n\n💡 Возможные причины:\n• Неверный код авторизации\n• Код уже использован\n• Проблемы с подключением к GitHub";
            }
            finally
            {
                IsLoading = false;
                SecureTokenStorage.ClearTempState();
            }
        }
    }
}