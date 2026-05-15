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
    public class CompetitionsViewModel : BaseViewModel
    {
        public bool doDispose = true;
        private bool _isInitialized = false;

        private readonly CompetitionService _competitionService;
        private readonly IExcelExportService _excelExportService;
        private readonly UserService _userService;

        private ObservableCollection<CompetitionDto> _competitions = new();
        private ObservableCollection<CompetitionDto> _filteredCompetitions = new();
        private ObservableCollection<StatusFilter> _statusFilters = new();
        private StatusFilter _selectedStatusFilter;

        public ObservableCollection<CompetitionDto> Competitions
        {
            get => _competitions;
            set
            {
                SetProperty(ref _competitions, value);
                OnPropertyChanged(nameof(HasNoCompetitions));
                ApplyFilter();
            }
        }

        public ObservableCollection<CompetitionDto> FilteredCompetitions
        {
            get => _filteredCompetitions;
            set => SetProperty(ref _filteredCompetitions, value);
        }

        public ObservableCollection<StatusFilter> StatusFilters
        {
            get => _statusFilters;
            set => SetProperty(ref _statusFilters, value);
        }

        public StatusFilter SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                SetProperty(ref _selectedStatusFilter, value);
                ApplyFilter();
            }
        }

        public int FilteredCompetitionsCount => FilteredCompetitions?.Count ?? 0;
        public bool HasNoCompetitions => Competitions?.Count == 0;
        public bool HasNoFilteredCompetitions => FilteredCompetitions?.Count == 0 && !HasNoCompetitions;
        public bool IsOrganizer { get; private set; }

        public ICommand AddCompetitionCommand { get; }
        public ICommand SelectCompetitionCommand { get; }
        public ICommand ExportCompetitionCommand { get; }
        public ICommand EditCompetitionCommand { get; }
        public ICommand ArchiveCompetitionCommand { get; }
        public ICommand DeleteCompetitionCommand { get; }
        public ICommand RefreshCommand { get; }

        public CompetitionsViewModel()
        {
            _excelExportService = new ExcelExportService();
            _competitionService = new CompetitionService();
            _userService = new UserService();

            AddCompetitionCommand = new RelayCommand(ExecuteAddCompetition);
            SelectCompetitionCommand = new RelayCommand<CompetitionDto>(ExecuteSelectCompetition, c => c != null);
            EditCompetitionCommand = new RelayCommand<CompetitionDto>(ExecuteEditCompetition, c => c != null && IsOrganizer);
            DeleteCompetitionCommand = new AsyncRelayCommand<CompetitionDto>(ExecuteDeleteCompetitionAsync, c => c != null && IsOrganizer);
            ExportCompetitionCommand = new AsyncRelayCommand<CompetitionDto>(ExecuteExportCompetitionAsync, c => c != null && IsOrganizer);
            ArchiveCompetitionCommand = new AsyncRelayCommand<CompetitionDto>(ExecuteArchiveCompetitionAsync, c => c != null && IsOrganizer && !c.IsArchived);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => true);

            InitializeStatusFilters();
        }

        public async Task InitializeAsync()
        {
            await LoadCompetitionsAsync();
            await CheckUserRole();
        }

        public async Task RefreshAsync()
        {
            await LoadCompetitionsAsync();
        }

        private void ExecuteAddCompetition()
        {
            doDispose = false;
            _navigationService.NavigateTo(new EditCompetitionPage(null));
        }

        private void ExecuteEditCompetition(CompetitionDto competition)
        {
            doDispose = false;

            if (competition != null)
                _navigationService.NavigateTo(new EditCompetitionPage(competition));
        }

        private void ExecuteSelectCompetition(CompetitionDto competition)
        {
            doDispose = false;

            if (competition != null)
                _navigationService.NavigateTo(new CompetitionDetailsPage(competition));
        }

        private async Task ExecuteDeleteCompetitionAsync(CompetitionDto competition)
        {
            var result = await ShowYesNoCancelAsync($"Вы уверены, что хотите удалить соревнование \"{competition.Name}\"?\n\n" +
                "При удалении соревнования будут удалены:\n" +
                "• Все команды соревнований\n" +
                "• Все задачи и чаты команд\n" +
                "• Все этапы расписания\n" +
                "• Все результаты и фиксации составов\n\n" +
                "Это действие нельзя отменить!",
                "Подтверждение удаления");

            if (result != true) return;

            var deleteResult = await _competitionService.DeleteCompetitionAsync(competition.Id);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (deleteResult.Success)
                {
                    await ShowSuccessAsync($"Соревнование \"{competition.Name}\" успешно удалено!");
                    await LoadCompetitionsAsync();
                }
                else
                {
                    await ShowErrorAsync(deleteResult.Message);
                }
            });
        }

        private async Task ExecuteArchiveCompetitionAsync(CompetitionDto competition)
        {
            var result = await ShowYesNoCancelAsync($"Вы уверены, что хотите архивировать соревнование \"{competition.Name}\"?\n\n" +
                    "При архивировании будут удалены:\n" +
                    "• Все этапы расписания\n" +
                    "• Все чаты и сообщения команд\n" +
                    "• Все задачи команд\n" +
                    "• Все участники будут откреплены от команд\n\n" +
                    "Результаты и финальные составы команд останутся в архиве.\n\n" +
                    "Это действие нельзя отменить!",
                    "Архивирование соревнования");

            if (result != true) return;

            var archiveResult = await _competitionService.ArchiveCompetitionAsync(competition.Id);

            if (archiveResult.Success)
            {
                await ShowSuccessAsync($"Соревнование \"{competition.Name}\" успешно архивировано!");
                await LoadCompetitionsAsync();
            }
            else
            {
               await ShowErrorAsync(archiveResult.Message);
            }
        }

        private async Task ExecuteExportCompetitionAsync(CompetitionDto competition)
        {
            var exportDataResponse = await _competitionService.GetCompetitionExportDataAsync(competition.Id);
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
            else
            {
                await ShowErrorAsync("Ошибка при экспорте данных");
            }
        }

        private async Task LoadCompetitionsAsync()
        {
            IsLoading = true;

            var competitionsResponse = await _competitionService.GetCompetitionsAsync();
            if (!competitionsResponse.Success)
            {
                await ShowErrorAsync(competitionsResponse.Message);
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Competitions = new ObservableCollection<CompetitionDto>(competitionsResponse.Data);
            });
            IsLoading = false;
        }

        private async Task CheckUserRole()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user.Success)
            {
                IsOrganizer = user.Data.RoleId == (int)Roles.Organizer || user.Data.RoleId == (int)Roles.Admin;
                OnPropertyChanged(nameof(IsOrganizer));
            }
        }

        private void InitializeStatusFilters()
        {
            StatusFilters = new ObservableCollection<StatusFilter>
            {
                new StatusFilter { Id = 0, Name = "Все соревнования", StatusType = CompetitionStatusType.All },
                new StatusFilter { Id = 1, Name = "Активные", StatusType = CompetitionStatusType.Active },
                new StatusFilter { Id = 2, Name = "Завершенные", StatusType = CompetitionStatusType.Completed },
                new StatusFilter { Id = 3, Name = "Предстоящие", StatusType = CompetitionStatusType.Upcoming },
                new StatusFilter { Id = 4, Name = "Архивные", StatusType = CompetitionStatusType.Archived }
            };

            SelectedStatusFilter = StatusFilters.First();
        }

        private void ApplyFilter()
        {
            if (Competitions == null || SelectedStatusFilter == null)
            {
                FilteredCompetitions = new ObservableCollection<CompetitionDto>(Competitions ?? new());
                return;
            }

            var filtered = Competitions.Where(c => ShouldIncludeCompetition(c, SelectedStatusFilter.StatusType));
            FilteredCompetitions = new ObservableCollection<CompetitionDto>(filtered);

            OnPropertyChanged(nameof(FilteredCompetitionsCount));
            OnPropertyChanged(nameof(HasNoFilteredCompetitions));
        }

        private bool ShouldIncludeCompetition(CompetitionDto competition, CompetitionStatusType? statusType)
        {
            if (statusType == null || statusType == CompetitionStatusType.All)
                return true;

            if (statusType == CompetitionStatusType.Archived)
                return competition.IsArchived;

            if (competition.IsArchived) return false;

            var now = DateTime.Now;
            return statusType switch
            {
                CompetitionStatusType.Active => competition.StartDate <= now && competition.EndDate >= now,
                CompetitionStatusType.Completed => competition.EndDate < now,
                CompetitionStatusType.Upcoming => competition.StartDate > now,
                _ => true
            };
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose) return;
            base.DisposeManagedResources();

            Competitions?.Clear();
            FilteredCompetitions?.Clear();
            StatusFilters?.Clear();

            if (_competitionService is IDisposable compDisposable) compDisposable.Dispose();
            if (_userService is IDisposable userDisposable) userDisposable.Dispose();
            if (_excelExportService is IDisposable excelDisposable) excelDisposable.Dispose();
        }
    }

    public class StatusFilter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CompetitionStatusType? StatusType { get; set; }
    }

    public enum CompetitionStatusType
    {
        All,
        Active,
        Completed,
        Upcoming,
        Archived
    }
}