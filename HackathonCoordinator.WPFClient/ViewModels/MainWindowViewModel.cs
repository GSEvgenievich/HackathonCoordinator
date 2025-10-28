using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly TeamService _teamService;
        private readonly UserService _userService;

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public RelayCommand OpenProfileCommand { get; }
        public RelayCommand OpenMainPageCommand { get; }
        public RelayCommand ToggleThemeCommand { get; }

        public MainWindowViewModel()
        {
            _navigationService = App.NavigationService;
            _teamService = new TeamService();
            _userService = new UserService();

            ToggleThemeCommand = new RelayCommand(App.ToggleTheme);
            OpenMainPageCommand = new RelayCommand(OpenMainPage);
            OpenProfileCommand = new RelayCommand(() =>
                _navigationService.NavigateTo(new ProfilePage()));

            GetUsername();
        }

        private async void GetUsername()
        {
            var user = await _userService.GetCurrentUserAsync();
            Username = user.Username;
        }

        private void OpenMainPage()
        {
            var teamId = _teamService.GetCurrentTeamIdAsync();

            if (teamId == null)
                _navigationService.NavigateTo(new NoTeamPage());
            else
                _navigationService.NavigateTo(new TeamPage());
        }
    }
}
