using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class CompetitionDetailsViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly CompetitionService _competitionService;
        private readonly TeamService _teamService;
        private readonly UserService _userService;

        private CompetitionDto _competition;
        public CompetitionDto Competition
        {
            get => _competition;
            set => SetProperty(ref _competition, value);
        }

        private string _inviteCode = "";
        public string InviteCode
        {
            get => _inviteCode;
            set => SetProperty(ref _inviteCode, value);
        }

        public bool IsOrganizer { get; private set; }
        public bool IsRegularUser => !IsOrganizer;

        public RelayCommand BackCommand { get; }
        public RelayCommand CreateTeamCommand { get; }
        public RelayCommand JoinTeamCommand { get; }
        public RelayCommand<TeamDto> DeleteTeamCommand { get; }
        public RelayCommand<TeamDto> ManageTeamCommand { get; }

        public CompetitionDetailsViewModel()
        {
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _teamService = new TeamService();
            _userService = new UserService();

            BackCommand = new RelayCommand(() => BackToCompetitions());
            CreateTeamCommand = new RelayCommand(async () => await CreateTeamAsync());
            JoinTeamCommand = new RelayCommand(async () => await JoinTeamAsync());
            DeleteTeamCommand = new RelayCommand<TeamDto>(async (team) => await DeleteTeamAsync(team));
            ManageTeamCommand = new RelayCommand<TeamDto>((team) => ManageTeam(team));

            CheckUserRole();
        }

        private void BackToCompetitions()
        {
            _navigationService.NavigateTo(new CompetitionsPage());
        }

        private async void LoadCompetitionAsync(int competitionId)
        {
            var competition = await _competitionService.GetCompetitionAsync(competitionId);
            if (competition != null)
            {
                Competition = competition;
            }
            else
            {
                MessageBox.Show("Соревнование не найдено");
                _navigationService.NavigateTo(new CompetitionsPage());
            }
        }

        private async void CheckUserRole()
        {
            var user = await _userService.GetCurrentUserAsync();
            IsOrganizer = user?.RoleId == 3;
            OnPropertyChanged(nameof(IsOrganizer));
            OnPropertyChanged(nameof(IsRegularUser));
        }

        private async Task CreateTeamAsync()
        {
            var teamName = await ShowInputDialogAsync("Введите название команды:");
            if (string.IsNullOrWhiteSpace(teamName)) return;

            var result = await _competitionService.CreateTeamAsync(Competition.Id, teamName);
            MessageBox.Show(result.Message);

            if (result.Success)
            {
                LoadCompetitionAsync(Competition.Id);
            }
        }

        private async Task JoinTeamAsync()
        {
            if (string.IsNullOrWhiteSpace(InviteCode))
            {
                MessageBox.Show("Введите код приглашения");
                return;
            }

            var result = await _teamService.JoinTeamAsync(InviteCode);
            MessageBox.Show(result.Message);

            if (result.Success)
            {
                _navigationService.NavigateTo(new TeamPage());
            }
        }

        private async Task DeleteTeamAsync(TeamDto team)
        {
            if (team == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить команду \"{team.Name}\"? Это действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var deleteResult = await _competitionService.DeleteTeamAsync(team.Id);
                MessageBox.Show(deleteResult.Message);

                if (deleteResult.Success)
                {
                    // Обновляем список команд
                    LoadCompetitionAsync(Competition.Id);
                }
            }
        }

        private void ManageTeam(TeamDto team)
        {
            if (team != null)
            {
                _navigationService.NavigateTo(new TeamPage(team.Id));
            }
        }

        private async Task<string> ShowInputDialogAsync(string prompt)
        {
            return Microsoft.VisualBasic.Interaction.InputBox(prompt, "Создание команды", "");
        }
    }
}