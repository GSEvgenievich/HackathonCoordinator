using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly TeamService _teamService;
        private readonly UserService _userService;
        private readonly AuthService _authService;

        private readonly string[] _themes = { "Light", "Dark", "Summer", "Spring", "Winter", "Autumn" };
        private int _currentThemeIndex = 0;

        private bool _isOrganizer = false;
        public bool IsOrganizer
        {
            get => _isOrganizer;
            set => SetProperty(ref _isOrganizer, value);
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string CurrentThemeName => _themes[_currentThemeIndex];

        public ICommand OpenProfileCommand { get; }
        public ICommand OpenMainPageCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenUsersManagementCommand { get; }
        public ICommand OpenChatsCommand {  get; }

        public MainWindowViewModel()
        {
            _navigationService = App.NavigationService;
            _teamService = new TeamService();
            _userService = new UserService();
            _authService = new AuthService();

            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            OpenMainPageCommand = new RelayCommand(OpenMainPage);
            OpenChatsCommand = new RelayCommand(() => _navigationService.NavigateTo(new ChatsPage()));
            OpenProfileCommand = new RelayCommand(() => _navigationService.NavigateTo(new ProfilePage()));
            LogoutCommand = new RelayCommand(ExecuteLogout);
            OpenUsersManagementCommand = new RelayCommand(() => ExecuteOpenUsersManagement());
        }

        public async void CheckUserRole()
        {
            var user = await _userService.GetCurrentUserAsync();
            IsOrganizer = user.Data.RoleId == 3; 
            OnPropertyChanged(nameof(IsOrganizer));
        }

        private void ToggleTheme()
        {
            _currentThemeIndex = (_currentThemeIndex + 1) % _themes.Length;
            App.SwitchTheme(_themes[_currentThemeIndex]);
            OnPropertyChanged(nameof(CurrentThemeName));
        }

        private void ExecuteOpenUsersManagement()
        {
            App.NavigationService.NavigateTo(new UsersManagementPage());
        }

        public async void GetUsername()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                Username = user.Data.Username ?? "Гость";
            }
            catch
            {
                Username = "Гость";
            }
        }

        private async void OpenMainPage()
        {
            var teamId = await _teamService.GetCurrentTeamIdAsync();

            if (!teamId.Success)
                _navigationService.NavigateTo(new CompetitionsPage());
            else
                _navigationService.NavigateTo(new TeamPage());
        }

        private void ExecuteLogout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение выхода",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();
                _navigationService.NavigateTo(new AuthorizationPage());
            }
        }
    }
}