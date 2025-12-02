using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;

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

        // AsyncRelayCommand для операций с API
        public ICommand BackCommand { get; }
        public ICommand CreateTeamCommand { get; }
        public ICommand JoinTeamCommand { get; }
        public ICommand GoToMainCommand { get; }
        public ICommand DeleteTeamCommand { get; }
        public ICommand ManageTeamCommand { get; }
        public ICommand ExportCompetitionCommand { get; }

        public CompetitionDetailsViewModel()
        {
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _excelExportService = new ExcelExportService();
            _teamService = new TeamService();
            _userService = new UserService();

            BackCommand = new RelayCommand(BackToCompetitions);

            // AsyncRelayCommand для создания команды
            CreateTeamCommand = new AsyncRelayCommand(
                execute: async () => await CreateTeamAsync(),
                canExecute: () => Competition != null && !IsAlreadyInTeam && IsOrganizer);

            GoToMainCommand = new RelayCommand(GoToMainPage);

            // AsyncRelayCommand для присоединения к команде
            JoinTeamCommand = new AsyncRelayCommand(
                execute: async () => await JoinTeamAsync(),
                canExecute: () => !string.IsNullOrWhiteSpace(InviteCode) && CanJoinTeam);

            // AsyncRelayCommand для удаления команды
            DeleteTeamCommand = new AsyncRelayCommand<TeamDto>(
                execute: async (team) => await DeleteTeamAsync(team),
                canExecute: (team) => team != null && IsOrganizer);

            ManageTeamCommand = new RelayCommand<TeamDto>(ManageTeam);

            // AsyncRelayCommand для экспорта
            ExportCompetitionCommand = new AsyncRelayCommand(
                execute: async () => await ExportCompetitionAsync(),
                canExecute: () => Competition != null && IsOrganizer);

            CheckUserStatus();
        }

        private async void CheckUserStatus()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                IsOrganizer = user.Data.RoleId == 3;
                IsAlreadyInTeam = user.Data.TeamId != null;

                OnPropertyChanged(nameof(IsOrganizer));
                OnPropertyChanged(nameof(IsRegularUser));
                OnPropertyChanged(nameof(IsAlreadyInTeam));
                OnPropertyChanged(nameof(CanJoinTeam));
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка проверки статуса пользователя: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void BackToCompetitions()
        {
            _navigationService.NavigateTo(new CompetitionsPage());
        }

        private void GoToMainPage()
        {
            _navigationService.NavigateTo(new TeamPage());
        }

        public async void LoadCompetitionAsync(int competitionId)
        {
            try
            {
                var competition = await _competitionService.GetCompetitionAsync(competitionId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (competition.Success)
                    {
                        Competition = competition.Data;
                    }
                    else
                    {
                        MessageBox.Show($"Соревнование не найдено: {competition.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        _navigationService.NavigateTo(new CompetitionsPage());
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки соревнования: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    _navigationService.NavigateTo(new CompetitionsPage());
                });
            }
        }

        private async Task CreateTeamAsync()
        {
            var teamName = await ShowInputDialogAsync("Введите название команды:");
            if (string.IsNullOrWhiteSpace(teamName)) return;

            try
            {
                var result = await _competitionService.CreateTeamAsync(Competition.Id, teamName);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(result.Message,
                        result.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (result.Success)
                    {
                        LoadCompetitionAsync(Competition.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка создания команды: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task JoinTeamAsync()
        {
            try
            {
                var result = await _teamService.JoinTeamAsync(InviteCode);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(result.Message,
                        result.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (result.Success)
                    {
                        _navigationService.NavigateTo(new TeamPage());
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка присоединения к команде: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task DeleteTeamAsync(TeamDto team)
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    $"Вы уверены, что хотите удалить команду \"{team.Name}\"? Это действие нельзя отменить.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var deleteResult = await _competitionService.DeleteTeamAsync(team.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(deleteResult.Message,
                        deleteResult.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        deleteResult.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (deleteResult.Success)
                    {
                        LoadCompetitionAsync(Competition.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка удаления команды: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
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
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return Microsoft.VisualBasic.Interaction.InputBox(prompt, "Создание команды", "");
            });
        }

        private async Task ExportCompetitionAsync()
        {
            try
            {
                var exportDataResponse = await _competitionService.GetCompetitionExportDataAsync(Competition.Id);

                if (exportDataResponse.Success)
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        FileName = exportDataResponse.Data.SuggestedFileName,
                        Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                        DefaultExt = ".xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        var success = await _excelExportService.ExportCompetitionToExcelAsync(
                            exportDataResponse.Data, saveFileDialog.FileName);

                        if (success)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                MessageBox.Show($"Данные соревнования успешно экспортированы в Excel файл:\n{saveFileDialog.FileName}",
                                    "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                    }
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("Ошибка при получении данных для экспорта", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            Competition = null;
            _inviteCode = null;

            if (_competitionService is IDisposable compDisposable)
                compDisposable.Dispose();

            if (_teamService is IDisposable teamDisposable)
                teamDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();

            if (_excelExportService is IDisposable excelDisposable)
                excelDisposable.Dispose();
        }
    }
}