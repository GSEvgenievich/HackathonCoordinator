using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.Storages;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class NoTeamViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly TeamService _teamService;
        private readonly UserService _userService;

        private string _teamName;
        private string _inviteCode;
        private bool _linkToGitHub;
        private bool _hasGitHubAccount;

        public string TeamName
        {
            get => _teamName;
            set => SetProperty(ref _teamName, value);
        }

        public string InviteCode
        {
            get => _inviteCode;
            set => SetProperty(ref _inviteCode, value);
        }

        public bool LinkToGitHub
        {
            get => _linkToGitHub;
            set => SetProperty(ref _linkToGitHub, value);
        }

        public bool HasGitHubAccount
        {
            get => _hasGitHubAccount;
            set => SetProperty(ref _hasGitHubAccount, value);
        }

        public bool ShowGitHubWarning => !HasGitHubAccount;

        public RelayCommand CreateTeamCommand { get; }
        public RelayCommand JoinTeamCommand { get; }

        public NoTeamViewModel()
        {
            _navigationService = App.NavigationService;
            _teamService = new TeamService();
            _userService = new UserService();

            CreateTeamCommand = new RelayCommand(async () => await ExecuteCreateTeamAsync());
            JoinTeamCommand = new RelayCommand(async () => await ExecuteJoinTeamAsync());

            CheckGitHubStatus();
        }

        private async void CheckGitHubStatus()
        {
            var user = await _userService.GetCurrentUserAsync();
            HasGitHubAccount = user?.GitHubUsername != null;
        }

        private async Task ExecuteCreateTeamAsync()
        {
            if (string.IsNullOrWhiteSpace(TeamName))
            {
                MessageBox.Show("Введите название команды");
                return;
            }

            if (LinkToGitHub && !HasGitHubAccount)
            {
                MessageBox.Show("Для привязки команды к GitHub необходимо сначала привязать GitHub аккаунт в профиле");
                return;
            }

            var result = await _teamService.CreateTeamAsync(TeamName, LinkToGitHub);
            MessageBox.Show(result.Message);

            if (result.Success)
                _navigationService.NavigateTo(new TeamPage());
        }

        private async Task ExecuteJoinTeamAsync()
        {
            if (string.IsNullOrWhiteSpace(InviteCode))
            {
                MessageBox.Show("Введите код приглашения");
                return;
            }

            var result = await _teamService.JoinTeamAsync(InviteCode);
            MessageBox.Show(result.Message);

            if (result.Success)
                _navigationService.NavigateTo(new TeamPage());
        }
    }
}