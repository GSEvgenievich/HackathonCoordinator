using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class CompetitionResultsViewModel : BaseViewModel
    {
        private readonly IPdfExportService _pdfExportService;
        private readonly NavigationService _navigationService;
        private readonly CompetitionService _competitionService;
        private readonly UserService _userService;

        private CompetitionDto _competition;
        private ObservableCollection<TeamResultDto> _teams = new();
        private TeamResultDto _selectedTeam;
        private string _commentText;
        private bool _showCommentDialog;
        private bool _isEditMode;
        private bool _canMoveUp;
        private bool _canMoveDown;
        private bool _hasExistingResults;
        private bool _isArchived;
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
                {
                    UpdateMoveButtonsState();
                }
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
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _userService = new UserService();

            BackCommand = new RelayCommand(GoBack);
            SaveResultsCommand = new AsyncRelayCommand(SaveResultsAsync, () => IsEditMode && !IsArchived);
            ExportResultsCommand = new AsyncRelayCommand(ExportResults);
            MoveUpCommand = new RelayCommand(MoveUp, () => CanMoveUp && IsEditMode && !IsArchived);
            MoveDownCommand = new RelayCommand(MoveDown, () => CanMoveDown && IsEditMode && !IsArchived);
            AddCommentCommand = new RelayCommand<TeamResultDto>(ShowCommentDialogCommand);
            SaveCommentCommand = new AsyncRelayCommand(SaveCommentAsync);
            CancelCommentCommand = new RelayCommand(CancelComment);
            SelectTeamCommand = new RelayCommand<TeamResultDto>(SelectTeam);
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

        private void SelectTeam(TeamResultDto team)
        {
            SelectedTeam = team;
        }

        private void MoveUp()
        {
            if (SelectedTeam == null || !IsEditMode || IsArchived) return;

            var index = Teams.IndexOf(SelectedTeam);
            if (index > 0)
            {
                MoveTeam(index, index - 1);
            }
        }

        private void MoveDown()
        {
            if (SelectedTeam == null || !IsEditMode || IsArchived) return;

            var index = Teams.IndexOf(SelectedTeam);
            if (index < Teams.Count - 1)
            {
                MoveTeam(index, index + 1);
            }
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

        public async Task LoadCompetitionAsync(CompetitionDto competition, bool editMode = false, bool isOrganizer = false)
        {
            Competition = competition;
            IsOrganizer = isOrganizer;

            // Если соревнование в архиве - режим только просмотра
            if (IsArchived)
            {
                IsEditMode = false;
            }
            else
            {
                IsEditMode = editMode;
            }

            try
            {
                var competitionData = await _competitionService.GetCompetitionAsync(competition.Id);
                if (!competitionData.Success) return;

                var savedResults = await _competitionService.GetCompetitionResultsAsync(competition.Id);

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
                else
                {
                    HasExistingResults = false;

                    if (IsEditMode && !IsArchived)
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
                    else
                    {
                        if (!IsArchived)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                MessageBox.Show("Результаты еще не опубликованы", "Информация",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                GoBack();
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
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

            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    HasExistingResults
                        ? "Сохранить изменения результатов?\n\nПосле сохранения новые места будут зафиксированы."
                        : "Сохранить результаты соревнования?\n\nПосле сохранения порядок мест будет зафиксирован и станет доступен для просмотра участниками.",
                    "Подтверждение сохранения",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var saveResult = await _competitionService.SaveAllResultsAsync(Competition.Id, Teams.ToList());

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (saveResult.Success)
                    {
                        foreach (var team in Teams)
                        {
                            team.IsSaved = true;
                        }

                        HasExistingResults = true;

                        var updatedCompetition = await _competitionService.GetCompetitionAsync(Competition.Id);
                        if (updatedCompetition.Success)
                        {
                            Competition = updatedCompetition.Data;
                        }

                        MessageBox.Show("Результаты успешно сохранены!", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(saveResult.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task ExportResults()
        {
            if (Competition == null || Teams == null || !Teams.Any())
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("Нет данных для экспорта", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = $"Результаты_{Competition.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
                    DefaultExt = ".pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var success = await _pdfExportService.ExportResultsToPdfAsync(Competition, Teams.ToList(), saveFileDialog.FileName);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (success)
                        {
                            MessageBox.Show($"Результаты успешно экспортированы в PDF файл:\n{saveFileDialog.FileName}",
                                "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при создании PDF файла", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
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

        private void GoBack()
        {
            if (Competition != null)
            {
                _navigationService.NavigateTo(new CompetitionDetailsPage(Competition));
            }
            else
            {
                _navigationService.GoBack();
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            Teams?.Clear();

            if (_competitionService is IDisposable compDisposable)
                compDisposable.Dispose();
            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();
        }
    }
}