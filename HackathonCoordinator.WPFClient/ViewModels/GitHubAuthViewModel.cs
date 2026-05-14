using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.Storages;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class GitHubAuthViewModel : BaseViewModel
    {
        private readonly GitHubOAuthService _gitHubService;
        private readonly UserService _userService;

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _authorizationCode;
        public string AuthorizationCode
        {
            get => _authorizationCode;
            set => SetProperty(ref _authorizationCode, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }

        public bool IsNotLoading => !IsLoading;

        private bool _showCodeInput;
        public bool ShowCodeInput
        {
            get => _showCodeInput;
            set => SetProperty(ref _showCodeInput, value);
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        private string _currentGitHubAccount;
        public string CurrentGitHubAccount
        {
            get => _currentGitHubAccount;
            set => SetProperty(ref _currentGitHubAccount, value);
        }

        // AsyncRelayCommand для всех операций
        public ICommand StartOAuthCommand { get; }
        public ICommand ConfirmCodeCommand { get; }
        public ICommand GoToProfileCommand { get; }
        public ICommand ChangeAccountCommand { get; }

        public GitHubAuthViewModel()
        {
            _gitHubService = new GitHubOAuthService();
            _userService = new UserService();

            StartOAuthCommand = new AsyncRelayCommand(
                execute: async () => await StartOAuthFlowAsync(),
                canExecute: () => !IsLoading);

            ConfirmCodeCommand = new AsyncRelayCommand(
                execute: async () => await ConfirmAuthorizationCodeAsync(),
                canExecute: () => !IsLoading && !string.IsNullOrWhiteSpace(AuthorizationCode));

            GoToProfileCommand = new RelayCommand(() => _navigationService.NavigateTo(new ProfilePage()));

            ChangeAccountCommand = new AsyncRelayCommand(
                execute: async () => await ChangeGitHubAccountAsync(),
                canExecute: () => !IsLoading && !string.IsNullOrEmpty(CurrentGitHubAccount));

            LoadCurrentGitHubInfo();
        }

        private async void LoadCurrentGitHubInfo()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
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
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Ошибка загрузки информации GitHub: {ex.Message}";
                    ShowCodeInput = false;
                });
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

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
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
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"❌ Ошибка: {ex.Message}";
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ChangeGitHubAccountAsync()
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите отвязать текущий GitHub аккаунт и привязать новый?",
                    "Смена GitHub аккаунта",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                var unlinkResult = await _userService.UnlinkGitHubAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (unlinkResult.Success)
                    {
                        CurrentGitHubAccount = null;
                        StatusMessage = "✅ Текущий GitHub аккаунт отвязан. Теперь вы можете привязать новый аккаунт.";
                        ShowCodeInput = false;
                        _ = StartOAuthFlowAsync(); // Запускаем процесс привязки нового аккаунта
                    }
                    else
                    {
                        StatusMessage = $"❌ Ошибка отвязки: {unlinkResult.Message}";
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"❌ Ошибка: {ex.Message}";
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ConfirmAuthorizationCodeAsync()
        {
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

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (linkResult.Success)
                    {
                        StatusMessage = $"✅ GitHub аккаунт успешно привязан!\n\n👤 Имя пользователя: {result.Data.UserInfo.Login}\n📧 Email: {result.Data.UserInfo.Email ?? "Не указан"}";
                        CurrentGitHubAccount = result.Data.UserInfo.Login;
                        IsSuccess = true;
                        ShowCodeInput = false;

                        AuthorizationCode = string.Empty;
                    }
                    else
                    {
                        StatusMessage = $"❌ Ошибка привязки: {linkResult.Message}";
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"❌ Ошибка: {ex.Message}\n\n💡 Возможные причины:\n• Неверный код авторизации\n• Код уже использован\n• Проблемы с подключением к GitHub";
                });
            }
            finally
            {
                IsLoading = false;
                SecureTokenStorage.ClearTempState();
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            StatusMessage = null;
            AuthorizationCode = null;
            CurrentGitHubAccount = null;

            if (_gitHubService is IDisposable gitHubDisposable)
                gitHubDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();
        }
    }
}