using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly UserService _userService;
        private readonly AuthService _authService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _teamName;
        public string TeamName
        {
            get => _teamName;
            set { _teamName = value; OnPropertyChanged(); }
        }

        private string _currentIconPath;
        public string CurrentIconPath
        {
            get => _currentIconPath;
            set { _currentIconPath = value; OnPropertyChanged(); }
        }

        private bool _isIconPanelVisible;
        public bool IsIconPanelVisible
        {
            get => _isIconPanelVisible;
            set { _isIconPanelVisible = value; OnPropertyChanged(); }
        }

        // GitHub свойства
        private string _gitHubUsername;
        public string GitHubUsername
        {
            get => _gitHubUsername;
            set 
            {
                _gitHubUsername = value; 
                OnPropertyChanged(); 
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

        public ObservableCollection<IconDto> AvailableIcons { get; set; } = new ObservableCollection<IconDto>();

        private IconDto _selectedIcon;
        public IconDto SelectedIcon
        {
            get => _selectedIcon;
            set { _selectedIcon = value; OnPropertyChanged(); }
        }

        public RelayCommand ChangeIconCommand { get; }
        public RelayCommand<IconDto> SelectIconCommand { get; }
        public RelayCommand SaveProfileCommand { get; }
        public RelayCommand LogoutCommand { get; }
        public RelayCommand LinkGitHubCommand { get; }

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

            SaveProfileCommand = new RelayCommand(async () =>
            {
                if (SelectedIcon != null)
                {
                    var updateResult = await _userService.UpdateProfileAsync(Username, SelectedIcon.Id);

                    if (updateResult.Success)
                    {
                        MessageBox.Show("Профиль обновлён!");
                        LoadUserDataAsync();
                    }
                    else MessageBox.Show(updateResult.Message);
                }
            });

            LogoutCommand = new RelayCommand(() =>
            {
                _authService.Logout();
                _navigationService.NavigateTo(new AuthorizationPage());
            });

            LinkGitHubCommand = new RelayCommand(async () =>
            {
                if (string.IsNullOrEmpty(GitHubUsername))
                {
                    // Переходим на страницу привязки GitHub
                    _navigationService.NavigateTo(new GitHubAuthPage());
                }
                else
                {
                    // Отвязываем GitHub
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите отвязать аккаунт {GitHubUsername}?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var unlinkResult = await _userService.UnlinkGitHubAsync();
                        if (unlinkResult.Success)
                        {
                            GitHubUsername = null;
                            MessageBox.Show("GitHub аккаунт отвязан");
                        }
                        else
                        {
                            MessageBox.Show(unlinkResult.Message);
                        }
                    }
                }
            });

            LoadUserDataAsync();
        }

        private async void LoadUserDataAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
            {
                MessageBox.Show("Не удалось загрузить данные пользователя");
                return;
            }

            Username = user.Data.Username;
            Email = user.Data.Email;
            TeamName = user.Data.TeamName;
            GitHubUsername = user.Data.GitHubUsername;
            CurrentIconPath = user.Data.IconName != null ? $"/Assets/Images/Profile/{user.Data.IconName}.png" : "/Assets/Images/Profile/robot1.png";

            var icons = await _userService.GetAllIconsAsync();
            AvailableIcons.Clear();
            foreach (var icon in icons.Data)
            {
                if (icon.Id == user.Data.IconId)
                {
                    icon.IsSelected = true;
                }

                AvailableIcons.Add(icon);
                if (icon.Name == user.Data.IconName) SelectedIcon = icon;
            }
        }
    }
}