using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class CompetitionResultsViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly CompetitionService _competitionService;
        private readonly UserService _userService;

        private CompetitionDto _competition;
        private ObservableCollection<TeamResultDto> _teams = new();
        private TeamResultDto _selectedTeam;
        private string _commentText;
        private bool _showCommentDialog;
        private bool _isEditMode;  // Режим редактирования (для организатора/админа)
        private bool _canMoveUp;
        private bool _canMoveDown;
        private bool _hasExistingResults;  // Есть ли уже сохраненные результаты

        public CompetitionDto Competition
        {
            get => _competition;
            set
            {
                if (SetProperty(ref _competition, value))
                {
                    OnPropertyChanged(nameof(CompetitionName));
                }
            }
        }

        public string CompetitionName => Competition?.Name ?? "";
        public string PageTitle => IsEditMode ? "🏆 Подведение итогов" : "📊 Результаты соревнования";

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
                }
            }
        }

        public bool IsViewMode => !IsEditMode;
        public bool HasExistingResults => _hasExistingResults;

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
            _navigationService = App.NavigationService;
            _competitionService = new CompetitionService();
            _userService = new UserService();

            BackCommand = new RelayCommand(GoBack);
            SaveResultsCommand = new AsyncRelayCommand(SaveResultsAsync);
            ExportResultsCommand = new RelayCommand(ExportResults);  // Заглушка
            MoveUpCommand = new RelayCommand(MoveUp, () => CanMoveUp && IsEditMode);
            MoveDownCommand = new RelayCommand(MoveDown, () => CanMoveDown && IsEditMode);
            AddCommentCommand = new RelayCommand<TeamResultDto>(ShowCommentDialogCommand);
            SaveCommentCommand = new AsyncRelayCommand(SaveCommentAsync);
            CancelCommentCommand = new RelayCommand(CancelComment);
            SelectTeamCommand = new RelayCommand<TeamResultDto>(SelectTeam);
        }

        private void UpdateMoveButtonsState()
        {
            if (SelectedTeam == null || Teams == null || !IsEditMode)
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
            if (SelectedTeam == null || !IsEditMode) return;

            var index = Teams.IndexOf(SelectedTeam);
            if (index > 0)
            {
                MoveTeam(index, index - 1);
            }
        }

        private void MoveDown()
        {
            if (SelectedTeam == null || !IsEditMode) return;

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

        public async Task LoadCompetitionAsync(CompetitionDto competition, bool editMode = false)
        {
            Competition = competition;
            IsEditMode = editMode;

            try
            {
                var competitionData = await _competitionService.GetCompetitionAsync(competition.Id);
                if (!competitionData.Success) return;

                // Загружаем сохраненные результаты
                var savedResults = await _competitionService.GetCompetitionResultsAsync(competition.Id);

                var teams = new List<TeamResultDto>();

                if (savedResults.Success && savedResults.Data.Any())
                {
                    _hasExistingResults = true;

                    // Загружаем сохраненные результаты в правильном порядке мест
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
                    _hasExistingResults = false;

                    if (IsEditMode)
                    {
                        // Режим редактирования, но результатов нет - перемешиваем
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
                        // Режим просмотра, но результатов нет
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show("Результаты еще не опубликованы", "Информация",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            GoBack();
                        });
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
            if (SelectedTeam == null) return;

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
            if (!IsEditMode) return;

            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    _hasExistingResults
                        ? "Сохранить изменения результатов?\n\nПосле сохранения новые места будут зафиксированы."
                        : "Сохранить результаты соревнования?\n\nПосле сохранения порядок мест будет зафиксирован и станет доступен для просмотра участниками.",
                    "Подтверждение сохранения",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Сохраняем все результаты
                var saveResult = await _competitionService.SaveAllResultsAsync(Competition.Id, Teams.ToList());

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (saveResult.Success)
                    {
                        // Обновляем флаг сохранения для всех команд
                        foreach (var team in Teams)
                        {
                            team.IsSaved = true;
                        }
                        _hasExistingResults = true;

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

        private void ExportResults()
        {
            // Заглушка для экспорта результатов
            MessageBox.Show("Функция экспорта результатов будет добавлена в следующей версии",
                "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
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