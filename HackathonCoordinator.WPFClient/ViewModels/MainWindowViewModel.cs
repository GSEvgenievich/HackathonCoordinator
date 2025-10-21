using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;

        public RelayCommand OpenProfileCommand { get; }
        public RelayCommand ToggleThemeCommand { get; }

        public MainWindowViewModel()
        {
            _navigationService = App.NavigationService;

            ToggleThemeCommand = new RelayCommand(App.ToggleTheme);
            OpenProfileCommand = new RelayCommand(() =>
                _navigationService.NavigateTo(new ProfilePage()));
        }
    }
}
