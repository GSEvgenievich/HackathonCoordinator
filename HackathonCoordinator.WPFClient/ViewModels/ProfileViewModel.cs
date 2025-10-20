using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

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

            Username = user.Username;
            Email = user.Email;
            TeamName = user.TeamName;
            CurrentIconPath = user.IconName != null ? $"/Assets/Images/Profile/{user.IconName}.png" : "/Assets/Images/Profile/robot1.png";

            var icons = await _userService.GetAllIconsAsync();
            AvailableIcons.Clear();
            foreach (var icon in icons)
            {
                if (icon.Id == user.IconId)
                {
                    icon.IsSelected = true;
                }

                AvailableIcons.Add(icon);
                if (icon.Name == user.IconName) SelectedIcon = icon;
            }
        }
    }
}
