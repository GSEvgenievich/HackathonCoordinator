using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class CompetitionDetailsViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly CompetitionService _competitionService;
        private readonly IExcelExportService _excelExportService;
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

        private bool _isAlreadyInTeam;
        public bool IsAlreadyInTeam
        {
            get => _isAlreadyInTeam;
            set => SetProperty(ref _isAlreadyInTeam, value);
        }

        public bool IsOrganizer { get; private set; }
        public bool IsRegularUser => !IsOrganizer;
        public bool CanJoinTeam => IsRegularUser && !IsAlreadyInTeam;

        public RelayCommand BackCommand { get; }
        public RelayCommand CreateTeamCommand { get; }
        public RelayCommand JoinTeamCommand { get; }
        public RelayCommand GoToMainCommand { get; }
        public RelayCommand<TeamDto> DeleteTeamCommand { get; }
        public RelayCommand<TeamDto> ManageTeamCommand { get; }
        public RelayCommand ExportCompetitionCommand { get; }

        public CompetitionDetailsViewModel()
        {
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _excelExportService = new ExcelExportService();
            _teamService = new TeamService();
            _userService = new UserService();

            BackCommand = new RelayCommand(() => BackToCompetitions());
            CreateTeamCommand = new RelayCommand(async () => await CreateTeamAsync());
            GoToMainCommand = new RelayCommand(() => GoToMainPage());
            JoinTeamCommand = new RelayCommand(async () => await JoinTeamAsync());
            DeleteTeamCommand = new RelayCommand<TeamDto>(async (team) => await DeleteTeamAsync(team));
            ManageTeamCommand = new RelayCommand<TeamDto>((team) => ManageTeam(team));
            ExportCompetitionCommand = new RelayCommand(async () => await ExportCompetitionAsync());

            CheckUserStatus();
        }

        private async void CheckUserStatus()
        {
            var user = await _userService.GetCurrentUserAsync();
            IsOrganizer = user?.RoleId == 3;
            IsAlreadyInTeam = user?.TeamId != null;
            OnPropertyChanged(nameof(IsOrganizer));
            OnPropertyChanged(nameof(IsRegularUser));
            OnPropertyChanged(nameof(IsAlreadyInTeam));
            OnPropertyChanged(nameof(CanJoinTeam));
        }

        private void BackToCompetitions()
        {
            _navigationService.NavigateTo(new CompetitionsPage());
        }

        private void GoToMainPage()
        {
            _navigationService.NavigateTo(new TeamPage());
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

        private async Task ExportCompetitionAsync()
        {
            if (Competition == null) return;

            try
            {
                var exportData = await _competitionService.GetCompetitionExportDataAsync(Competition.Id);

                if (exportData != null)
                {
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = exportData.SuggestedFileName, 
                        Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                        DefaultExt = ".xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        var success = await _excelExportService.ExportCompetitionToExcelAsync(exportData, saveFileDialog.FileName);

                        if (success)
                        {
                            MessageBox.Show($"Данные соревнования успешно экспортированы в Excel файл:\n{saveFileDialog.FileName}",
                                "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка при получении данных для экспорта", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}