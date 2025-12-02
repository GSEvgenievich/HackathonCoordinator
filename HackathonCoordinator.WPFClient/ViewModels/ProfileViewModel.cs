using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class ProfileViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly UserService _userService;
        private readonly AuthService _authService;

        private string _username;
        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                ((AsyncRelayCommand)SaveProfileCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _teamName;
        public string TeamName
        {
            get => _teamName;
            set => SetProperty(ref _teamName, value);
        }

        private string _currentIconPath;
        public string CurrentIconPath
        {
            get => _currentIconPath;
            set => SetProperty(ref _currentIconPath, value);
        }

        private bool _isIconPanelVisible;
        public bool IsIconPanelVisible
        {
            get => _isIconPanelVisible;
            set => SetProperty(ref _isIconPanelVisible, value);
        }

        private string _gitHubUsername;
        public string GitHubUsername
        {
            get => _gitHubUsername;
            set
            {
                SetProperty(ref _gitHubUsername, value);
                OnPropertyChanged(nameof(GitHubStatus));
                OnPropertyChanged(nameof(GitHubStatusColor));
                OnPropertyChanged(nameof(GitHubButtonText));
            }
        }

        public string GitHubStatus => string.IsNullOrEmpty(GitHubUsername)
            ? "Не привязан"
            : $"Привязан ({GitHubUsername})";

        public Brush GitHubStatusColor => string.IsNullOrEmpty(GitHubUsername)
            ? Brushes.Red
            : Brushes.Green;

        public string GitHubButtonText => string.IsNullOrEmpty(GitHubUsername)
            ? "Привязать GitHub"
            : "Отвязать GitHub";

        public ObservableCollection<IconDto> AvailableIcons { get; set; } = new();

        private IconDto _selectedIcon;
        public IconDto SelectedIcon
        {
            get => _selectedIcon;
            set
            {
                SetProperty(ref _selectedIcon, value);
                ((AsyncRelayCommand)SaveProfileCommand)?.RaiseCanExecuteChanged();
            }
        }

        // Команды с AsyncRelayCommand
        public ICommand ChangeIconCommand { get; }
        public RelayCommand<IconDto> SelectIconCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand LinkGitHubCommand { get; }

        public ProfileViewModel()
        {
            _navigationService = App.NavigationService;
            _userService = new UserService();
            _authService = new AuthService();

            ChangeIconCommand = new RelayCommand(() => IsIconPanelVisible = !IsIconPanelVisible);
            SelectIconCommand = new RelayCommand<IconDto>(icon =>
            {
                if (icon == null) return;
                SelectedIcon = icon;
                CurrentIconPath = icon.Path;
                IsIconPanelVisible = false;
            });

            // AsyncRelayCommand для сохранения профиля
            SaveProfileCommand = new AsyncRelayCommand(
                execute: async () => await SaveProfileAsync(),
                canExecute: () => SelectedIcon != null && !string.IsNullOrWhiteSpace(Username));

            // AsyncRelayCommand для выхода
            LogoutCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteLogoutAsync(),
                canExecute: () => true);

            // AsyncRelayCommand для привязки GitHub
            LinkGitHubCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteLinkGitHubAsync(),
                canExecute: () => true);

            LoadUserDataAsync();
        }

        private async Task SaveProfileAsync()
        {
            try
            {
                var updateResult = await _userService.UpdateProfileAsync(Username, SelectedIcon.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (updateResult.Success)
                    {
                        MessageBox.Show("Профиль обновлён!", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                            {
                                mainViewModel.GetUsername();
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(updateResult.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

                if (updateResult.Success)
                {
                    await LoadUserDataAsync();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка обновления профиля: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task ExecuteLinkGitHubAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(GitHubUsername))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _navigationService.NavigateTo(new GitHubAuthPage());
                    });
                }
                else
                {
                    var result = await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        return MessageBox.Show(
                            $"Вы уверены, что хотите отвязать аккаунт {GitHubUsername}?",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    });

                    if (result == MessageBoxResult.Yes)
                    {
                        var unlinkResult = await _userService.UnlinkGitHubAsync();

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (unlinkResult.Success)
                            {
                                GitHubUsername = null;
                                MessageBox.Show("GitHub аккаунт отвязан", "Успешно",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show(unlinkResult.Message, "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка при работе с GitHub: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null || !user.Success)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("Не удалось загрузить данные пользователя", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                Username = user.Data.Username;
                Email = user.Data.Email;
                TeamName = user.Data.TeamName;
                GitHubUsername = user.Data.GitHubUsername;
                CurrentIconPath = user.Data.IconName != null
                    ? $"/Assets/Images/Profile/{user.Data.IconName}.png"
                    : "/Assets/Images/Profile/robot1.png";

                var icons = await _userService.GetAllIconsAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableIcons.Clear();

                    foreach (var icon in icons.Data)
                    {
                        if (icon.Id == user.Data.IconId)
                        {
                            icon.IsSelected = true;
                            SelectedIcon = icon;
                        }

                        AvailableIcons.Add(icon);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки данных профиля: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task ExecuteLogoutAsync()
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                        {
                            await mainViewModel.DisposeNotificationHub();
                        }
                    }

                    _authService.Logout();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _navigationService.NavigateTo(new AuthorizationPage());
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Ошибка при выходе: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            AvailableIcons?.Clear();
            SelectedIcon = null;

            Username = null;
            Email = null;
            TeamName = null;
            CurrentIconPath = null;
            GitHubUsername = null;

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();

            if (_authService is IDisposable authDisposable)
                authDisposable.Dispose();
        }
    }
}