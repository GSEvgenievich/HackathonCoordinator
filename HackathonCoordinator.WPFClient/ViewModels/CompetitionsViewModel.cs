using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class CompetitionsViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
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

        public RelayCommand AddCompetitionCommand { get; }
        public RelayCommand<CompetitionDto> SelectCompetitionCommand { get; }
        public RelayCommand<CompetitionDto> ExportCompetitionCommand { get; }
        public RelayCommand<CompetitionDto> EditCompetitionCommand { get; }

        public CompetitionsViewModel()
        {
            _excelExportService = new ExcelExportService();
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _userService = new UserService();

            AddCompetitionCommand = new RelayCommand(() => AddCompetition());
            SelectCompetitionCommand = new RelayCommand<CompetitionDto>(SelectCompetition);
            EditCompetitionCommand = new RelayCommand<CompetitionDto>(EditCompetition);
            ExportCompetitionCommand = new RelayCommand<CompetitionDto>(async (competition) => await ExportCompetitionAsync(competition));

            InitializeStatusFilters();
            LoadCompetitionsAsync();
            CheckUserRole();
        }

        private void InitializeStatusFilters()
        {
            StatusFilters = new ObservableCollection<StatusFilter>
            {
                new StatusFilter { Id = 0, Name = "Любой статус" },
                new StatusFilter { Id = 1, Name = "Активные", StatusType = CompetitionStatusType.Active },
                new StatusFilter { Id = 2, Name = "Завершенные", StatusType = CompetitionStatusType.Completed },
                new StatusFilter { Id = 3, Name = "Предстоящие", StatusType = CompetitionStatusType.Upcoming },
                new StatusFilter { Id = 4, Name = "Все соревнования", StatusType = CompetitionStatusType.All }
            };

            SelectedStatusFilter = StatusFilters.First();
        }

        private void ApplyFilter()
        {
            if (Competitions == null || SelectedStatusFilter == null)
            {
                FilteredCompetitions = new ObservableCollection<CompetitionDto>(Competitions ?? new ObservableCollection<CompetitionDto>());
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

            var now = DateTime.Now;

            return statusType switch
            {
                CompetitionStatusType.Active => competition.StartDate <= now && competition.EndDate >= now,
                CompetitionStatusType.Completed => competition.EndDate < now,
                CompetitionStatusType.Upcoming => competition.StartDate > now,
                _ => true
            };
        }

        private async void LoadCompetitionsAsync()
        {
            var competitions = await _competitionService.GetCompetitionsAsync();

            if (competitions == null)
            {
                Competitions = new ObservableCollection<CompetitionDto>();
            }
            else
            {
                if (IsOrganizer)
                {
                    // Организатор видит все соревнования
                    Competitions = new ObservableCollection<CompetitionDto>(competitions);
                }
                else
                {
                    // Обычный пользователь видит только активные
                    var activeCompetitions = competitions.Where(c => c.IsActive && c.EndDate >= DateTime.Now).ToList();
                    Competitions = new ObservableCollection<CompetitionDto>(activeCompetitions);
                }
            }

            ApplyFilter();
        }

        private async void CheckUserRole()
        {
            var user = await _userService.GetCurrentUserAsync();
            IsOrganizer = user?.RoleId == 3; // 3 = Organizer
            OnPropertyChanged(nameof(IsOrganizer));
        }

        private void AddCompetition()
        {
            _navigationService.NavigateTo(new EditCompetitionPage(null));
        }

        private void SelectCompetition(CompetitionDto competition)
        {
            if (competition != null)
            {
                _navigationService.NavigateTo(new CompetitionDetailsPage(competition));
            }
        }

        private void EditCompetition(CompetitionDto competition)
        {
            if (competition != null)
            {
                _navigationService.NavigateTo(new EditCompetitionPage(competition));
            }
        }

        private async Task ExportCompetitionAsync(CompetitionDto competition)
        {
            if (competition == null) return;

            try
            {
                var exportData = await _competitionService.GetCompetitionExportDataAsync(competition.Id);

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
        Upcoming
    }
}