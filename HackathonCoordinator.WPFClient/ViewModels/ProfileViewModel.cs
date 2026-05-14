using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class ProfileViewModel : BaseViewModel
    {
        public bool doDispose = true;
        private bool _isInitialized = false;

        private readonly UserService _userService;
        private readonly AuthService _authService;
        private readonly PositionService _positionService;

        private UserProfileExtendedDto _user;
        private int _targetUserId;
        private bool _isOwnProfile;

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
        public ICommand SelectIconCommand { get; }
        public ICommand SaveUsernameCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand LinkGitHubCommand { get; }
        public ICommand ViewTeamCompositionCommand { get; }

        public ProfileViewModel()
        {
            _userService = new UserService();
            _authService = new AuthService();
            _positionService = new PositionService();

            BackCommand = new RelayCommand(GoBack);
            ChangeIconCommand = new RelayCommand(() => IsIconPanelVisible = !IsIconPanelVisible);
            SelectIconCommand = new AsyncRelayCommand<IconDto>(SelectIcon);
            SaveUsernameCommand = new AsyncRelayCommand(SaveUsernameAsync);
            LogoutCommand = new AsyncRelayCommand(ExecuteLogoutAsync);
            LinkGitHubCommand = new AsyncRelayCommand(ExecuteLinkGitHubAsync);
            ViewTeamCompositionCommand = new RelayCommand<UserResultDto>(ShowTeamComposition);
        }

        public async Task InitializeAsync(int? userId = null)
        {
            if (_isInitialized) return;

            IsLoading = true;

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

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки профиля: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshAsync()
        {
            _isInitialized = false;
            await InitializeAsync(_targetUserId);
        }

        private async Task LoadPositionsAsync()
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

        private async Task LoadIconsAsync()
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

                if (User.IconId == null && AvailableIcons.Any())
                {
                    AvailableIcons[0].IsSelected = true;
                }
            }
        }

        private async Task SavePositionAsync()
        {
            if (SelectedPosition == null || SelectedPosition.Id == User.PositionId) return;

            var result = await _userService.UpdateUserPositionAsync(SelectedPosition.Id);
            if (result.Success)
            {
                User.PositionId = SelectedPosition.Id;
                User.PositionName = SelectedPosition.Name;
            }
            else
            {
                await ShowErrorAsync(result.Message);
            }
        }

        private async Task SaveUsernameAsync()
        {
            if (string.IsNullOrWhiteSpace(EditUsername) || EditUsername == User.Username) return;

            var result = await _userService.UpdateProfileAsync(EditUsername, User.IconId);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (result.Success)
                {
                    User.Username = EditUsername;

                    if (Application.Current.MainWindow is MainWindow mainWindow &&
                        mainWindow.DataContext is MainWindowViewModel mainViewModel)
                    {
                        mainViewModel.GetUsername();
                    }
                }
                else
                {
                    MessageBox.Show(result.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private async Task SelectIcon(IconDto icon)
        {
            if (icon == null) return;

            var result = await _userService.UpdateProfileAsync(User.Username, icon.Id);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (result.Success)
                {
                    if (SelectedIcon != null) SelectedIcon.IsSelected = false;

                    SelectedIcon = icon;
                    User.IconName = icon.Name;
                    User.IconId = icon.Id;

                    if (_navigationService.CurrentPage is ProfilePage profilePage)
                    {
                        var newPath = $"pack://application:,,,/Assets/Images/Profile/{icon.Name}.png";
                        profilePage.UpdateAvatar(newPath);
                    }

                    IsIconPanelVisible = false;
                    icon.IsSelected = true;
                }
                else
                {
                    MessageBox.Show(result.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private async Task ExecuteLinkGitHubAsync()
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
                    if (unlinkResult.Success)
                    {
                        User.GitHubUsername = null;
                        OnPropertyChanged(nameof(GitHubButtonText));
                        OnPropertyChanged(nameof(User.GitHubUsername));
                        OnPropertyChanged(nameof(User.GitHubStatus));
                    }
                    else
                    {
                        await ShowErrorAsync(unlinkResult.Message);
                    }
                }
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
        }


        private void GoBack()
        {
            _navigationService.GoBack();
        }

        private async Task NavigateToAuthAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _navigationService.NavigateTo(new AuthorizationPage());
            });
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose) return;

            base.DisposeManagedResources();
            AvailableIcons?.Clear();
            AvailablePositions?.Clear();

            if (_userService is IDisposable userDisposable) userDisposable.Dispose();
            if (_authService is IDisposable authDisposable) authDisposable.Dispose();
            if (_positionService is IDisposable positionDisposable) positionDisposable.Dispose();
        }
    }
}