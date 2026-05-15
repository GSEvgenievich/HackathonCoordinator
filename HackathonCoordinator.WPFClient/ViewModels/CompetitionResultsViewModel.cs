using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class CompetitionResultsViewModel : BaseViewModel
    {
        private readonly IPdfExportService _pdfExportService;
        private readonly CompetitionService _competitionService;
        private readonly UserService _userService;

        private bool _isInitialized = false;
        private int _competitionId;

        private CompetitionDto _competition;
        private ObservableCollection<TeamResultDto> _teams = new();
        private TeamResultDto _selectedTeam;
        private string _commentText;
        private bool _showCommentDialog;
        private bool _isEditMode;
        private bool _canMoveUp;
        private bool _canMoveDown;
        private bool _hasExistingResults;
        private bool _isOrganizer;

        public bool IsOrganizer
        {
            get => _isOrganizer;
            set => SetProperty(ref _isOrganizer, value);
        }

        public CompetitionDto Competition
        {
            get => _competition;
            set
            {
                if (SetProperty(ref _competition, value))
                {
                    OnPropertyChanged(nameof(CompetitionName));
                    OnPropertyChanged(nameof(IsArchived));
                    OnPropertyChanged(nameof(PageTitle));
                }
            }
        }

        public bool IsArchived => Competition?.IsArchived ?? false;
        public string CompetitionName => Competition?.Name ?? "";

        public string PageTitle
        {
            get
            {
                if (IsArchived) return "📦 Результаты (архив)";
                return IsEditMode ? "🏆 Подведение итогов" : "📊 Результаты соревнования";
            }
        }

        public ObservableCollection<TeamResultDto> Teams
        {
            get => _teams;
            set => SetProperty(ref _teams, value);
        }

        public TeamResultDto SelectedTeam
        {
            get => _selectedTeam;
            set
            {
                if (SetProperty(ref _selectedTeam, value))
                    UpdateMoveButtonsState();
            }
        }

        public bool CanMoveUp
        {
            get => _canMoveUp;
            set => SetProperty(ref _canMoveUp, value);
        }

        public bool CanMoveDown
        {
            get => _canMoveDown;
            set => SetProperty(ref _canMoveDown, value);
        }

        public string CommentText
        {
            get => _commentText;
            set => SetProperty(ref _commentText, value);
        }

        public bool ShowCommentDialog
        {
            get => _showCommentDialog;
            set => SetProperty(ref _showCommentDialog, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(IsViewMode));
                    OnPropertyChanged(nameof(PageTitle));
                }
            }
        }

        public bool IsViewMode => !IsEditMode;
        public bool HasExistingResults
        {
            get => _hasExistingResults;
            set => SetProperty(ref _hasExistingResults, value);
        }

        public string SaveButtonText => "💾 Сохранить результаты";
        public string ExportButtonText => "📤 Экспортировать результаты";

        // Команды
        public ICommand BackCommand { get; }
        public ICommand SaveResultsCommand { get; }
        public ICommand ExportResultsCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand AddCommentCommand { get; }
        public ICommand SaveCommentCommand { get; }
        public ICommand CancelCommentCommand { get; }
        public ICommand SelectTeamCommand { get; }

        // События для UI
        public event Action<int, int> TeamsReordered;
        public event Action<int> PositionUpdated;

        public CompetitionResultsViewModel()
        {
            _pdfExportService = new PdfExportService();
            _competitionService = new CompetitionService();
            _userService = new UserService();

            SaveResultsCommand = new AsyncRelayCommand(SaveResultsAsync, () => IsEditMode && !IsArchived);
            ExportResultsCommand = new AsyncRelayCommand(ExportResults);
            MoveUpCommand = new RelayCommand(MoveUp, () => CanMoveUp && IsEditMode && !IsArchived);
            MoveDownCommand = new RelayCommand(MoveDown, () => CanMoveDown && IsEditMode && !IsArchived);
            AddCommentCommand = new RelayCommand<TeamResultDto>(ShowCommentDialogCommand);
            SaveCommentCommand = new AsyncRelayCommand(SaveCommentAsync);
            CancelCommentCommand = new RelayCommand(CancelComment);
            SelectTeamCommand = new RelayCommand<TeamResultDto>(SelectTeam);
            BackCommand = new RelayCommand(() => _navigationService.GoBack());
        }

        /// <summary>
        /// Первичная инициализация с готовым объектом (при навигации)
        /// </summary>
        public async Task InitializeAsync(CompetitionDto competition, bool editMode = false, bool isOrganizer = false)
        {
            if (_isInitialized && _competitionId == competition.Id) return;

            _competitionId = competition.Id;
            Competition = competition;
            IsOrganizer = isOrganizer;
            IsEditMode = IsArchived ? false : editMode;

            IsLoading = true;

            await LoadCompetitionDataAsync();
            _isInitialized = true;

            IsLoading = false;
        }

        /// <summary>
        /// Обновление данных (после изменений)
        /// </summary>
        public async Task RefreshDataAsync()
        {
            if (Competition?.Id == null)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ShowErrorAsync("Не удалось обновить данные!");

                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                        {
                            await mainViewModel.OpenMainPage();
                        }
                    }
                });
                return;
            }

            var competitionData = await _competitionService.GetCompetitionAsync(Competition.Id);
            if (!competitionData.Success)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ShowErrorAsync(competitionData.Message);

                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                        {
                            await mainViewModel.OpenMainPage();
                        }
                    }
                });
                return;
            }

            Competition = competitionData.Data;
            await LoadCompetitionDataAsync();
        }

        private async Task LoadCompetitionDataAsync()
        {
            var competitionData = await _competitionService.GetCompetitionAsync(Competition.Id);
            if (!competitionData.Success)
            {
                await ShowErrorAsync(competitionData.Message);
                return;
            }

            var savedResults = await _competitionService.GetCompetitionResultsAsync(Competition.Id);
            var teams = new List<TeamResultDto>();

            if (savedResults.Success && savedResults.Data.Any())
            {
                HasExistingResults = true;

                foreach (var savedTeam in savedResults.Data.OrderBy(r => r.Place))
                {
                    var team = competitionData.Data.Teams.FirstOrDefault(t => t.Id == savedTeam.TeamId);
                    if (team != null)
                    {
                        savedTeam.TeamName = team.Name;
                        savedTeam.MembersCount = team.Members.Count.ToString();
                        savedTeam.IsSaved = true;
                        teams.Add(savedTeam);
                    }
                }
                Teams = new ObservableCollection<TeamResultDto>(teams);
            }
            else if (IsEditMode && !IsArchived)
            {
                var random = new Random();
                foreach (var team in competitionData.Data.Teams)
                {
                    teams.Add(new TeamResultDto
                    {
                        TeamId = team.Id,
                        TeamName = team.Name,
                        Place = null,
                        PlaceDisplay = "?",
                        Comment = "",
                        MembersCount = team.Members.Count.ToString(),
                        IsSaved = false
                    });
                }
                Teams = new ObservableCollection<TeamResultDto>(teams.OrderBy(x => random.Next()));
                UpdateAllPlaces();
            }
            else if (!IsArchived)
            {
                await ShowErrorAsync("Результаты еще не опубликованы");
                await Application.Current.Dispatcher.InvokeAsync(() => Back());
            }
        }

        private void UpdateMoveButtonsState()
        {
            if (SelectedTeam == null || Teams == null || !IsEditMode || IsArchived)
            {
                CanMoveUp = false;
                CanMoveDown = false;
                return;
            }

            var index = Teams.IndexOf(SelectedTeam);
            CanMoveUp = index > 0;
            CanMoveDown = index < Teams.Count - 1;
        }

        private void SelectTeam(TeamResultDto team) => SelectedTeam = team;

        private void MoveUp()
        {
            if (SelectedTeam == null || !IsEditMode || IsArchived) return;
            var index = Teams.IndexOf(SelectedTeam);
            if (index > 0) MoveTeam(index, index - 1);
        }

        private void MoveDown()
        {
            if (SelectedTeam == null || !IsEditMode || IsArchived) return;
            var index = Teams.IndexOf(SelectedTeam);
            if (index < Teams.Count - 1) MoveTeam(index, index + 1);
        }

        public void MoveTeam(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex) return;

            var team = Teams[fromIndex];
            Teams.Move(fromIndex, toIndex);
            UpdateAllPlaces();
            TeamsReordered?.Invoke(fromIndex, toIndex);
        }

        private void UpdateAllPlaces()
        {
            for (int i = 0; i < Teams.Count; i++)
            {
                var place = i + 1;
                Teams[i].Place = place;
                Teams[i].PlaceDisplay = place.ToString();
                Teams[i].IsSaved = false;
                PositionUpdated?.Invoke(i);
            }
            UpdateMoveButtonsState();
        }

        private void ShowCommentDialogCommand(TeamResultDto team)
        {
            SelectedTeam = team;
            CommentText = team.Comment;
            ShowCommentDialog = true;
        }

        private async Task SaveCommentAsync()
        {
            if (SelectedTeam == null || IsArchived) return;

            SelectedTeam.Comment = CommentText;
            ShowCommentDialog = false;
            CommentText = "";
        }

        private void CancelComment()
        {
            ShowCommentDialog = false;
            CommentText = "";
        }

        private async Task SaveResultsAsync()
        {
            if (!IsEditMode || IsArchived) return;

            var result = await ShowYesNoCancelAsync(HasExistingResults
                ? "Сохранить изменения результатов?\n\nПосле сохранения новые места будут зафиксированы."
                : "Сохранить результаты соревнования?\n\nПосле сохранения порядок мест будет зафиксирован и станет доступен для просмотра участниками.",
                "Подтверждение сохранения");

            if (result != true) return;

            var saveResult = await _competitionService.SaveAllResultsAsync(Competition.Id, Teams.ToList());
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (saveResult.Success)
                {
                    foreach (var team in Teams) team.IsSaved = true;
                    HasExistingResults = true;

                    var updatedCompetition = await _competitionService.GetCompetitionAsync(Competition.Id);
                    if (updatedCompetition.Success) Competition = updatedCompetition.Data;

                    await ShowSuccessAsync("Результаты успешно сохранены!");
                }
                else
                {
                    await ShowErrorAsync(saveResult.Message);
                }
            });
        }

        private async Task ExportResults()
        {
            if (Competition == null || Teams == null || !Teams.Any())
            {
                await ShowErrorAsync("Нет данных для экспорта");
                return;
            }

            var safeFileName = GenerateResultsFileName(Competition.Name);

            var saveFileDialog = new SaveFileDialog
            {
                FileName = safeFileName,
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
                DefaultExt = ".pdf"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            var success = await _pdfExportService.ExportResultsToPdfAsync(Competition, Teams.ToList(), saveFileDialog.FileName);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (success)
                {
                    await ShowInfoAsync($"Результаты успешно экспортированы в PDF файл:\n{saveFileDialog.FileName}", "Экспорт завершен");
                }
                else
                {
                    await ShowErrorAsync("Ошибка при создании PDF файла");
                }
            });
        }

        /// <summary>
        /// Генерация безопасного имени файла для результатов
        /// </summary>
        public static string GenerateResultsFileName(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                return $"results_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            var invalidChars = Path.GetInvalidFileNameChars();

            var safeName = new string(sourceName
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray());

            safeName = System.Text.RegularExpressions.Regex.Replace(safeName, @"[\""\'\`\~\@\$\%\^\&\*\(\)\=\+\{\}\[\]\|\\\/\?\<\>\:\;]", "_");

            safeName = safeName.Replace("\"", "_")
                               .Replace("'", "_")
                               .Replace("`", "_")
                               .Replace("«", "_")
                               .Replace("»", "_")
                               .Replace("„", "_")
                               .Replace("“", "_")
                               .Replace("”", "_");

            safeName = safeName.Trim();

            while (safeName.Contains("  "))
                safeName = safeName.Replace("  ", " ");

            const int maxNameLength = 50;
            if (safeName.Length > maxNameLength)
                safeName = safeName.Substring(0, maxNameLength).Trim();

            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "export";

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"results__{safeName}_{timestamp}.pdf";
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            Teams?.Clear();

            if (_competitionService is IDisposable compDisposable) compDisposable.Dispose();
            if (_userService is IDisposable userDisposable) userDisposable.Dispose();
        }
    }
}