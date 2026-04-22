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
        private readonly PositionService _positionService;

        private UserProfileExtendedDto _user;
        public UserProfileExtendedDto User
        {
            get => _user;
            set
            {
                SetProperty(ref _user, value);
                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(HasNoResults));
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(PageSubtitle));
            }
        }

        private int _targetUserId;
        private bool _isOwnProfile;

        public bool IsOwnProfile
        {
            get => _isOwnProfile;
            set => SetProperty(ref _isOwnProfile, value);
        }

        public string PageTitle => IsOwnProfile ? "Мой профиль" : $"Профиль: {User?.Username}";
        public string PageSubtitle => IsOwnProfile ? "Управление учетной записью" : "Информация об участнике";

        public bool HasResults => User?.Results?.Any() == true;
        public bool HasNoResults => !HasResults;

        // Для редактирования имени
        private string _editUsername;
        public string EditUsername
        {
            get => _editUsername;
            set => SetProperty(ref _editUsername, value);
        }

        // Для выбора должности
        private ObservableCollection<PositionDto> _availablePositions = new();
        public ObservableCollection<PositionDto> AvailablePositions
        {
            get => _availablePositions;
            set => SetProperty(ref _availablePositions, value);
        }

        private PositionDto _selectedPosition;
        public PositionDto SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                if (SetProperty(ref _selectedPosition, value) && IsOwnProfile && value != null && value.Id != User?.PositionId)
                {
                    SavePositionAsync();
                }
            }
        }

        // Для иконок
        private bool _isIconPanelVisible;
        public bool IsIconPanelVisible
        {
            get => _isIconPanelVisible;
            set => SetProperty(ref _isIconPanelVisible, value);
        }

        public ObservableCollection<IconDto> AvailableIcons { get; set; } = new();

        private IconDto _selectedIcon;
        public IconDto SelectedIcon
        {
            get => _selectedIcon;
            set => SetProperty(ref _selectedIcon, value);
        }

        public string GitHubButtonText => string.IsNullOrEmpty(User?.GitHubUsername)
            ? "Привязать GitHub"
            : "Отвязать GitHub";

        // Команды
        public ICommand BackCommand { get; }
        public ICommand ChangeIconCommand { get; }
        public RelayCommand<IconDto> SelectIconCommand { get; }
        public ICommand SaveUsernameCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand LinkGitHubCommand { get; }
        public ICommand ViewTeamCompositionCommand { get; }

        public ProfileViewModel()
        {
            _navigationService = App.NavigationService;
            _userService = new UserService();
            _authService = new AuthService();
            _positionService = new PositionService();

            BackCommand = new RelayCommand(GoBack);
            ChangeIconCommand = new RelayCommand(() => IsIconPanelVisible = !IsIconPanelVisible);
            SelectIconCommand = new RelayCommand<IconDto>(SelectIcon);
            SaveUsernameCommand = new AsyncRelayCommand(SaveUsernameAsync);
            LogoutCommand = new AsyncRelayCommand(ExecuteLogoutAsync);
            LinkGitHubCommand = new AsyncRelayCommand(ExecuteLinkGitHubAsync);
            ViewTeamCompositionCommand = new RelayCommand<UserResultDto>(ShowTeamComposition);
        }

        public async Task LoadProfileAsync(int? userId = null)
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (!currentUser.Success)
                {
                    await NavigateToAuthAsync();
                    return;
                }

                _targetUserId = userId ?? currentUser.Data.Id;
                _isOwnProfile = _targetUserId == currentUser.Data.Id;

                var result = await _userService.GetUserProfileExtendedAsync(_targetUserId);

                if (!result.Success)
                {
                    await ShowErrorAsync(result.Message);
                    return;
                }

                User = result.Data;
                EditUsername = User.Username;

                if (_isOwnProfile)
                {
                    await LoadPositionsAsync();
                    await LoadIconsAsync();
                }

                OnPropertyChanged(nameof(IsOwnProfile));
                OnPropertyChanged(nameof(GitHubButtonText));
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки профиля: {ex.Message}");
            }
        }

        private async Task LoadPositionsAsync()
        {
            try
            {
                var positions = await _positionService.GetAllPositionsAsync();
                if (positions.Success)
                {
                    AvailablePositions.Clear();
                    foreach (var pos in positions.Data)
                    {
                        AvailablePositions.Add(pos);
                        if (pos.Id == User.PositionId)
                        {
                            _selectedPosition = pos;
                        }
                    }
                    OnPropertyChanged(nameof(SelectedPosition));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки должностей: {ex.Message}");
            }
        }

        private async Task LoadIconsAsync()
        {
            try
            {
                var icons = await _userService.GetAllIconsAsync();
                if (icons.Success)
                {
                    AvailableIcons.Clear();
                    foreach (var icon in icons.Data)
                    {
                        if (icon.Id == User.IconId)
                        {
                            icon.IsSelected = true;
                            SelectedIcon = icon;
                            User.IconName = icon.Name;
                        }
                        AvailableIcons.Add(icon);
                    }

                    if (User.IconId == null)
                    {
                        AvailableIcons[0].IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки иконок: {ex.Message}");
            }
        }

        private async Task SavePositionAsync()
        {
            if (SelectedPosition == null || SelectedPosition.Id == User.PositionId) return;

            try
            {
                var result = await _userService.UpdateUserPositionAsync(SelectedPosition.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        User.PositionId = SelectedPosition.Id;
                        User.PositionName = SelectedPosition.Name;
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка обновления должности: {ex.Message}");
            }
        }

        private async Task SaveUsernameAsync()
        {
            if (string.IsNullOrWhiteSpace(EditUsername) || EditUsername == User.Username) return;

            try
            {
                var result = await _userService.UpdateProfileAsync(EditUsername, User.IconId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        User.Username = EditUsername;
                        MessageBox.Show("Имя пользователя обновлено!", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        if (Application.Current.MainWindow is MainWindow mainWindow &&
                            mainWindow.DataContext is MainWindowViewModel mainViewModel)
                        {
                            mainViewModel.GetUsername();
                        }
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка обновления имени: {ex.Message}");
            }
        }

        private void SelectIcon(IconDto icon)
        {
            if (icon == null) return;

            SelectedIcon.IsSelected = false;
            SelectedIcon = icon;
            User.IconName = icon.Name;
            User.IconId = icon.Id;
            IsIconPanelVisible = false;
            icon.IsSelected = true;

            _userService.UpdateProfileAsync(User.Username, icon.Id);
        }

        private async Task ExecuteLinkGitHubAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(User.GitHubUsername))
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
                            $"Вы уверены, что хотите отвязать аккаунт {User.GitHubUsername}?",
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
                                User.GitHubUsername = null;
                                OnPropertyChanged(nameof(GitHubButtonText));
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
                await ShowErrorAsync($"Ошибка при работе с GitHub: {ex.Message}");
            }
        }

        private void ShowTeamComposition(UserResultDto result)
        {
            if (result?.FinalTeamMembers == null) return;

            var compositionText = $"Состав команды \"{result.TeamName}\" на момент соревнования \"{result.CompetitionName}\":\n\n";

            foreach (var member in result.FinalTeamMembers)
            {
                var roleIcon = member.RoleName == "Капитан" ? "👑 " : "👤 ";
                compositionText += $"{roleIcon}{member.Username} - {member.PositionName} ({member.RoleName})\n";
            }

            MessageBox.Show(compositionText, $"Состав команды - {result.TeamName}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GoBack()
        {
            _navigationService.GoBack();
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
                    if (Application.Current.MainWindow is MainWindow mainWindow &&
                        mainWindow.DataContext is MainWindowViewModel mainViewModel)
                    {
                        await mainViewModel.DisposeNotificationHub();
                    }

                    _authService.Logout();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _navigationService.NavigateTo(new AuthorizationPage());
                    });
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync($"Ошибка при выходе: {ex.Message}");
                }
            }
        }

        private async Task NavigateToAuthAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _navigationService.NavigateTo(new AuthorizationPage());
            });
        }

        private async Task ShowErrorAsync(string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            AvailableIcons?.Clear();
            AvailablePositions?.Clear();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();
            if (_authService is IDisposable authDisposable)
                authDisposable.Dispose();
            if (_positionService is IDisposable positionDisposable)
                positionDisposable.Dispose();
        }
    }
}