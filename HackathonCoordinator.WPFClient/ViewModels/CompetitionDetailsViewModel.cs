using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class CompetitionDetailsViewModel : BaseViewModel
    {
        public bool doDispose = true;
        private bool _isInitialized = false;
        private int _competitionId;

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
                    OnPropertyChanged(nameof(IsArchived));
                    UpdateResultsButtonState();
                }
            }
        }

        private ObservableCollection<StageDto> _stages = new();
        public ObservableCollection<StageDto> Stages
        {
            get => _stages;
            set => SetProperty(ref _stages, value);
        }

        private ObservableCollection<DayStagesGroup> _stagesGroupedByDay;
        public ObservableCollection<DayStagesGroup> StagesGroupedByDay
        {
            get => _stagesGroupedByDay;
            set => SetProperty(ref _stagesGroupedByDay, value);
        }

        public bool IsArchived => Competition?.IsArchived ?? false;
        public bool HasNoStages => Stages == null || Stages.Count == 0;

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

        public string CompetitionStatusInfo => Competition == null ? "Неизвестно" :
            Competition.IsCompleted ? "✅ Закончено" :
            Competition.IsActive ? "🟢 Активно" : "⏳ Ожидает начала";

        // Команды
        public ICommand BackCommand { get; }
        public ICommand CreateTeamCommand { get; }
        public ICommand JoinTeamCommand { get; }
        public ICommand GoToMainCommand { get; }
        public ICommand DeleteTeamCommand { get; }
        public ICommand ManageTeamCommand { get; }
        public ICommand ExportCompetitionCommand { get; }
        public ICommand GoToResultsCommand { get; }
        public ICommand EditCompetitionCommand { get; }
        public ICommand DeleteCompetitionCommand { get; }
        public ICommand ArchiveCompetitionCommand { get; }

        public CompetitionDetailsViewModel()
        {
            _competitionService = new CompetitionService();
            _excelExportService = new ExcelExportService();
            _teamService = new TeamService();
            _userService = new UserService();


            BackCommand = new RelayCommand(GoBack);
            EditCompetitionCommand = new RelayCommand(EditCompetition);

            GoToMainCommand = new AsyncRelayCommand(GoToMainPageAsync);
            ManageTeamCommand = new AsyncRelayCommand<TeamDto>(ManageTeamAsync);
            DeleteCompetitionCommand = new AsyncRelayCommand(DeleteCompetitionAsync, () => Competition != null && IsOrganizer);
            ArchiveCompetitionCommand = new AsyncRelayCommand(ArchiveCompetitionAsync, () => Competition != null && IsOrganizer && !Competition.IsArchived);
            CreateTeamCommand = new AsyncRelayCommand(CreateTeamAsync, () => Competition != null && !IsAlreadyInTeam && IsOrganizer && !IsArchived);
            JoinTeamCommand = new AsyncRelayCommand(JoinTeamAsync, () => !string.IsNullOrWhiteSpace(InviteCode) && CanJoinTeam);
            DeleteTeamCommand = new AsyncRelayCommand<TeamDto>(DeleteTeamAsync, team => team != null && IsOrganizer && !IsArchived);
            ExportCompetitionCommand = new AsyncRelayCommand(ExportCompetitionAsync, () => Competition != null && IsOrganizer);
            GoToResultsCommand = new AsyncRelayCommand(GoToResultsAsync, () => CanGoToResults);
        }

        private void GoBack(object obj)
        {
            _navigationService.GoBack();
        }

        private async Task GoToMainPageAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                    {
                        await mainViewModel.OpenMainPage();
                    }
                }
            });
        }

        /// <summary>
        /// Первичная инициализация с готовым объектом (при навигации)
        /// </summary>
        public async Task InitializeAsync(CompetitionDto competition)
        {
            if (_isInitialized && Competition?.Id == competition.Id) return;

            _competitionId = competition.Id;
            Competition = competition;
            IsLoading = true;

            try
            {
                await LoadStagesAsync();
                await CheckUserStatus();
                OnPropertyChanged(nameof(CompetitionStatusInfo));
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки соревнования: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Обновление данных после изменений (по ID)
        /// </summary>
        public async Task LoadCompetitionAsync(int competitionId)
        {
            IsLoading = true;

            var competition = await _competitionService.GetCompetitionAsync(competitionId);
            if (!competition.Success)
            {
                await ShowErrorAsync($"Соревнование не найдено: {competition.Message}");
                return;
            }

            Competition = competition.Data;
            await LoadStagesAsync();
            await CheckUserStatus();
            OnPropertyChanged(nameof(CompetitionStatusInfo));

            IsLoading = false;
        }

        /// <summary>
        /// Обновление текущих данных
        /// </summary>
        public async Task RefreshAsync()
        {
            if (_competitionId > 0)
                await LoadCompetitionAsync(_competitionId);
        }

        private void UpdateResultsButtonState()
        {
            if (Competition == null) return;

            bool canEdit = IsOrganizer && Competition.IsCompleted && !Competition.IsArchived;
            bool canView = Competition.HasResults;

            if (canEdit)
            {
                CanGoToResults = true;
                ResultsButtonText = Competition.HasResults ? "✏️ Редактировать результаты" : "🏆 Подвести итоги";
                ResultsButtonTooltip = Competition.HasResults ? "Редактировать результаты соревнования" : "Подвести итоги соревнования";
            }
            else if (canView)
            {
                CanGoToResults = true;
                ResultsButtonText = "📊 Посмотреть результаты";
                ResultsButtonTooltip = "Просмотреть результаты соревнования";
            }
            else if (IsOrganizer)
            {
                CanGoToResults = false;
                ResultsButtonText = "🏆 Подвести итоги";
                ResultsButtonTooltip = $"❌ Нельзя подвести итоги, пока соревнование не закончено.\nСоревнование завершится: {Competition.EndDate:dd.MM.yyyy HH:mm}";
            }
            else
            {
                CanGoToResults = false;
                ResultsButtonText = "📊 Посмотреть результаты";
                ResultsButtonTooltip = "Итоги соревнования еще не подведены";
            }
        }

        private async Task CheckUserStatus()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user.Success)
            {
                IsOrganizer = user.Data.RoleId == (int)Roles.Organizer || user.Data.RoleId == (int)Roles.Admin;
                IsAlreadyInTeam = user.Data.TeamId != null;
                OnPropertyChanged(nameof(IsRegularUser));
                OnPropertyChanged(nameof(CanJoinTeam));
            }
        }

        private void EditCompetition()
        {
            doDispose = false;
            _navigationService.NavigateTo(new EditCompetitionPage(Competition));
        }

        private async Task ManageTeamAsync(TeamDto team)
        {
            if (team != null)
            {
                var teamResult = await _teamService.GetTeamByIdAsync(team.Id);

                if (teamResult.Success && teamResult.Data != null)
                {
                    doDispose = false;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _navigationService.NavigateTo(new TeamPage(teamResult.Data));
                    });
                }
                else
                {
                    await ShowErrorAsync($"Команда не найдена.\n{teamResult.Message}");
                }
            }
            else
            {
                await ShowErrorAsync($"Команда не найдена.");
            }
        }

        private async Task DeleteCompetitionAsync()
        {
            var result = await ShowYesNoCancelAsync($"Вы уверены, что хотите удалить соревнование \"{Competition.Name}\"?\n\n" +
                "При удалении соревнования будут удалены:\n" +
                "• Все команды соревнований\n" +
                "• Все задачи и чаты команд\n" +
                "• Все этапы расписания\n" +
                "• Все результаты и фиксации составов\n\n" +
                "Это действие нельзя отменить!",
                "Подтверждение удаления");

            if (result != true) return;

            var deleteResult = await _competitionService.DeleteCompetitionAsync(Competition.Id);

            if (deleteResult.Success)
            {
                await ShowSuccessAsync($"Соревнование \"{Competition.Name}\" успешно удалено!");
                Back();
            }
            else
            {
                await ShowErrorAsync(deleteResult.Message);
            }
        }

        private async Task ArchiveCompetitionAsync()
        {
            var result = await ShowYesNoCancelAsync($"Вы уверены, что хотите архивировать соревнование \"{Competition.Name}\"?\n\n" +
                "При архивировании будут удалены:\n" +
                "• Все этапы расписания\n" +
                "• Все чаты и сообщения команд\n" +
                "• Все задачи команд\n" +
                "• Все участники будут откреплены от команд\n\n" +
                "Результаты и финальные составы команд останутся в архиве.\n\n" +
                "Это действие нельзя отменить!",
                "Архивирование соревнования");

            if (result != true) return;

            var archiveResult = await _competitionService.ArchiveCompetitionAsync(Competition.Id);

            if (archiveResult.Success)
            {
                await ShowSuccessAsync($"Соревнование \"{Competition.Name}\" успешно архивировано!");
                await RefreshAsync();
            }
            else
            {
                await ShowErrorAsync(archiveResult.Message);
            }
        }

        private async Task LoadStagesAsync()
        {
            if (Competition == null) return;

            var result = await _competitionService.GetStagesAsync(Competition.Id);
            if (result.Success && result.Data != null)
            {
                Stages = new ObservableCollection<StageDto>(result.Data);

                var groups = result.Data
                    .OrderBy(s => s.StartTime)
                    .GroupBy(s => s.StartTime.Date)
                    .Select(g => new DayStagesGroup
                    {
                        DayHeader = g.Key.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("ru-RU")),
                        Stages = new ObservableCollection<StageDto>(g)
                    })
                    .ToList();

                StagesGroupedByDay = new ObservableCollection<DayStagesGroup>(groups);
                OnPropertyChanged(nameof(HasNoStages));
            }
        }

        private async Task CreateTeamAsync()
        {
            var teamName = await ShowInputDialogAsync("Создание команды", "Введите название команды:", "Команда");

            if (string.IsNullOrWhiteSpace(teamName)) return;

            var result = await _competitionService.CreateTeamAsync(Competition.Id, teamName);

            if (result.Success)
                await ShowSuccessAsync(result.Message);
            else
                await ShowErrorAsync(result.Message);

            if (result.Success)
                await RefreshAsync();
        }

        private async Task JoinTeamAsync()
        {
            var result = await _teamService.JoinTeamAsync(InviteCode);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (result.Success)
                    await ShowSuccessAsync(result.Message);
                else
                    await ShowErrorAsync(result.Message);

                if (result.Success)
                {
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                        {
                            await mainViewModel.OpenMainPage();
                        }
                    }
                }
            });
        }

        private async Task DeleteTeamAsync(TeamDto team)
        {
            var result = await ShowYesNoCancelAsync($"Вы уверены, что хотите удалить команду \"{team.Name}\"? Это действие нельзя отменить.", "Подтверждение удаления");

            if (result != true)
                return;

            var deleteResult = await _teamService.DeleteTeamAsync(team.Id);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (deleteResult.Success)
                    await ShowSuccessAsync(deleteResult.Message);
                else
                    await ShowErrorAsync(deleteResult.Message);

                if (deleteResult.Success)
                    await RefreshAsync();
            });
        }

        private async Task GoToResultsAsync()
        {
            if (!CanGoToResults) return;

            bool editMode = IsOrganizer && Competition.IsCompleted;
            doDispose = false;
            _navigationService.NavigateTo(new CompetitionResultsPage(Competition, editMode, IsOrganizer));
        }

        private async Task ExportCompetitionAsync()
        {
            var exportDataResponse = await _competitionService.GetCompetitionExportDataAsync(Competition.Id);
            if (!exportDataResponse.Success)
            {
                await ShowErrorAsync(exportDataResponse.Message);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                FileName = exportDataResponse.Data.SuggestedFileName,
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                DefaultExt = ".xlsx"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            var success = await _excelExportService.ExportCompetitionToExcelAsync(exportDataResponse.Data, saveFileDialog.FileName);
            if (success)
            {
                await ShowInfoAsync($"Данные соревнования успешно экспортированы в Excel файл:\n{saveFileDialog.FileName}", "Экспорт завершен");
            }
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose) return;
            base.DisposeManagedResources();

            Competition = null;
            _inviteCode = null;
            Stages?.Clear();
            StagesGroupedByDay?.Clear();

            if (_competitionService is IDisposable compDisposable) compDisposable.Dispose();
            if (_teamService is IDisposable teamDisposable) teamDisposable.Dispose();
            if (_userService is IDisposable userDisposable) userDisposable.Dispose();
            if (_excelExportService is IDisposable excelDisposable) excelDisposable.Dispose();
        }

        public class DayStagesGroup
        {
            public string DayHeader { get; set; }
            public ObservableCollection<StageDto> Stages { get; set; }
        }
    }
}