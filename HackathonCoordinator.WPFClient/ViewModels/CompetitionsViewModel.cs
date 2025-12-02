using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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

        // Все команды, которые обращаются к API, делаем асинхронными
        public ICommand AddCompetitionCommand { get; }
        public ICommand SelectCompetitionCommand { get; }
        public ICommand ExportCompetitionCommand { get; }
        public ICommand EditCompetitionCommand { get; }
        public ICommand RefreshCommand { get; }

        public CompetitionsViewModel()
        {
            _excelExportService = new ExcelExportService();
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _userService = new UserService();

            // Простые команды навигации
            AddCompetitionCommand = new RelayCommand(
                () => _navigationService.NavigateTo(new EditCompetitionPage(null)));

            SelectCompetitionCommand = new RelayCommand<CompetitionDto>(
                competition => ExecuteSelectCompetition(competition),
                competition => competition != null);

            EditCompetitionCommand = new RelayCommand<CompetitionDto>(
                competition => _navigationService.NavigateTo(new EditCompetitionPage(competition)),
                competition => competition != null && IsOrganizer);

            // Асинхронные команды с операциями
            ExportCompetitionCommand = new AsyncRelayCommand<CompetitionDto>(
                execute: async (competition) => await ExecuteExportCompetitionAsync(competition),
                canExecute: (competition) => competition != null && IsOrganizer);

            RefreshCommand = new AsyncRelayCommand(
                execute: async () => await LoadCompetitionsAsync(),
                canExecute: () => true);

            InitializeStatusFilters();
            LoadCompetitionsAsync(); // Запускаем при создании
            CheckUserRole();
        }

        private void ExecuteSelectCompetition(CompetitionDto competition)
        {
            if (competition != null)
            {
                _navigationService.NavigateTo(new CompetitionDetailsPage(competition));
            }
        }

        private async Task ExecuteExportCompetitionAsync(CompetitionDto competition)
        {
            try
            {
                var exportDataResponse = await _competitionService.GetCompetitionExportDataAsync(competition.Id);

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

        private async Task LoadCompetitionsAsync()
        {
            try
            {
                var competitionsResponse = await _competitionService.GetCompetitionsAsync();

                if (!competitionsResponse.Success)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Ошибка загрузки соревнований: {competitionsResponse.Message}", "Ошибка");
                    });
                }
                else
                {
                    Competitions = new ObservableCollection<CompetitionDto>(competitionsResponse.Data);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки соревнований: {ex.Message}", "Ошибка");
                });
            }
        }

        private async void CheckUserRole()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                IsOrganizer = user.Data.RoleId == 3; // 3 = Organizer
                OnPropertyChanged(nameof(IsOrganizer));
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка проверки роли: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        // Остальные методы без изменений...
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

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            Competitions?.Clear();
            FilteredCompetitions?.Clear();
            StatusFilters?.Clear();

            if (_competitionService is IDisposable compDisposable)
                compDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();

            if (_excelExportService is IDisposable excelDisposable)
                excelDisposable.Dispose();
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