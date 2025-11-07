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

        private readonly string[] _themes = { "Light", "Dark", "Summer", "Winter", "Autumn", "Spring" };
        private int _currentThemeIndex = 0;

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

        public MainWindowViewModel()
        {
            _navigationService = App.NavigationService;
            _teamService = new TeamService();
            _userService = new UserService();
            _authService = new AuthService();

            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            OpenMainPageCommand = new RelayCommand(OpenMainPage);
            OpenProfileCommand = new RelayCommand(() => _navigationService.NavigateTo(new ProfilePage()));
            LogoutCommand = new RelayCommand(ExecuteLogout);

            GetUsername();
        }

        private void ToggleTheme()
        {
            _currentThemeIndex = (_currentThemeIndex + 1) % _themes.Length;
            App.SwitchTheme(_themes[_currentThemeIndex]);
            OnPropertyChanged(nameof(CurrentThemeName));
        }

        private async void GetUsername()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                Username = user?.Username ?? "Гость";
            }
            catch
            {
                Username = "Гость";
            }
        }

        private async void OpenMainPage()
        {
            var teamId = await _teamService.GetCurrentTeamIdAsync();

            if (teamId == null)
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