using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
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
            set
            {
                if (SetProperty(ref _competition, value))
                {
                    // Обновляем свойства кнопки при изменении соревнования
                    UpdateResultsButtonState();
                }
            }
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

        private bool _isOrganizer;
        public bool IsOrganizer
        {
            get => _isOrganizer;
            set
            {
                if (SetProperty(ref _isOrganizer, value))
                {
                    UpdateResultsButtonState();
                }
            }
        }

        private bool _canGoToResults;
        public bool CanGoToResults
        {
            get => _canGoToResults;
            set => SetProperty(ref _canGoToResults, value);
        }

        private bool _canViewResults;
        public bool CanViewResults
        {
            get => _canViewResults;
            set => SetProperty(ref _canViewResults, value);
        }

        private string _resultsButtonText;
        public string ResultsButtonText
        {
            get => _resultsButtonText;
            set => SetProperty(ref _resultsButtonText, value);
        }

        private string _resultsButtonTooltip;
        public string ResultsButtonTooltip
        {
            get => _resultsButtonTooltip;
            set => SetProperty(ref _resultsButtonTooltip, value);
        }

        public bool IsRegularUser => !IsOrganizer;
        public bool CanJoinTeam => IsRegularUser && !IsAlreadyInTeam;

        public string CompetitionStatusInfo
        {
            get
            {
                if (Competition == null) return "Неизвестно";
                if (Competition.IsCompleted) return "✅ Закончено";
                if (Competition.IsActive) return "🟢 Активно";
                return "⏳ Ожидает начала";
            }
        }

        // Метод обновления состояния кнопки
        private void UpdateResultsButtonState()
        {
            if (Competition == null) return;

            var canEdit = IsOrganizer && Competition.IsCompleted;
            var canView = Competition.HasResults;

            CanGoToResults = canEdit || canView;

            if (canEdit)
            {
                ResultsButtonText = Competition.HasResults ? "✏️ Редактировать результаты" : "🏆 Подвести итоги";
                ResultsButtonTooltip = Competition.HasResults
                    ? "Редактировать результаты соревнования"
                    : "Подвести итоги соревнования";
            }
            else if (canView)
            {
                ResultsButtonText = "📊 Посмотреть результаты";
                ResultsButtonTooltip = "Просмотреть результаты соревнования";
            }
            else
            {
                ResultsButtonText = "🏆 Подвести итоги";
                ResultsButtonTooltip = "Соревнование еще не закончено. Итоги можно подвести после окончания.";
                CanGoToResults = false;
            }
        }

        // AsyncRelayCommand для операций с API
        public ICommand BackCommand { get; }
        public ICommand CreateTeamCommand { get; }
        public ICommand JoinTeamCommand { get; }
        public ICommand GoToMainCommand { get; }
        public ICommand DeleteTeamCommand { get; }
        public ICommand ManageTeamCommand { get; }
        public ICommand ExportCompetitionCommand { get; }
        public ICommand GoToResultsCommand { get; }

        public CompetitionDetailsViewModel()
        {
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _excelExportService = new ExcelExportService();
            _teamService = new TeamService();
            _userService = new UserService();

            BackCommand = new RelayCommand(BackToCompetitions);
            GoToMainCommand = new RelayCommand(GoToMainPage);
            ManageTeamCommand = new RelayCommand<TeamDto>(ManageTeam);

            // AsyncRelayCommand для создания команды
            CreateTeamCommand = new AsyncRelayCommand(
                execute: async () => await CreateTeamAsync(),
                canExecute: () => Competition != null && !IsAlreadyInTeam && IsOrganizer);

            // AsyncRelayCommand для присоединения к команде
            JoinTeamCommand = new AsyncRelayCommand(
                execute: async () => await JoinTeamAsync(),
                canExecute: () => !string.IsNullOrWhiteSpace(InviteCode) && CanJoinTeam);

            // AsyncRelayCommand для удаления команды
            DeleteTeamCommand = new AsyncRelayCommand<TeamDto>(
                execute: async (team) => await DeleteTeamAsync(team),
                canExecute: (team) => team != null && IsOrganizer);

            // AsyncRelayCommand для экспорта
            ExportCompetitionCommand = new AsyncRelayCommand(
                execute: async () => await ExportCompetitionAsync(),
                canExecute: () => Competition != null && IsOrganizer);

            // AsyncRelayCommand для перехода к подведению итогов
            GoToResultsCommand = new AsyncRelayCommand(
                execute: async () => await GoToResultsAsync(),
                canExecute: () => CanGoToResults);

            CheckUserStatus();
        }

        private async void CheckUserStatus()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                IsOrganizer = user.Data.RoleId == (int)Roles.Organizer || user.Data.RoleId == (int)Roles.Admin;
                IsAlreadyInTeam = user.Data.TeamId != null;

                OnPropertyChanged(nameof(IsRegularUser));
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
                        OnPropertyChanged(nameof(CompetitionStatusInfo));
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

        private async Task GoToResultsAsync()
        {
            if (!CanGoToResults) return;

            var resultsPage = new CompetitionResultsPage();
            var viewModel = resultsPage.DataContext as CompetitionResultsViewModel;

            if (viewModel != null)
            {
                bool editMode = IsOrganizer && Competition.IsCompleted;
                await viewModel.LoadCompetitionAsync(Competition, editMode);
                _navigationService.NavigateTo(resultsPage);
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