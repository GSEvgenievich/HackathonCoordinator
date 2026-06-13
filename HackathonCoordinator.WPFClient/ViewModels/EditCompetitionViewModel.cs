using HackathonCoordinator.ServiceLayer;
using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class EditCompetitionViewModel : BaseViewModel
    {
        private readonly CompetitionService _competitionService;

        private CompetitionDto? _competition = null;

        // Основные поля соревнования
        private string _name = "";
        private string _description = "";
        private DateTime _startDate = DateTime.Today;
        private DateTime _endDate = DateTime.Today.AddDays(1);
        private string _startTime = "10:00";
        private string _endTime = "18:00";
        private string _errorMessage = "";
        private bool _isEditMode = false;

        // Stages
        private ObservableCollection<StageEditItem> _stages = new();
        private bool _showStageDialog;
        private string _stageDialogName = "";
        private string _stageDialogDescription = "";
        private string _stageDialogLocation = "";
        private StageEditItem _editingStage;
        private bool _isStageEditMode;
        private DateTime _stageDialogStartDate = DateTime.Today;
        private string _stageDialogStartTime = "12:00";
        private bool _isStageStartTimeEditable;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    RecalculateAllStagesTimes();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    RecalculateAllStagesTimes();
                }
            }
        }

        public string StartTime
        {
            get => _startTime;
            set
            {
                if (SetProperty(ref _startTime, value))
                {
                    RecalculateAllStagesTimes();
                }
            }
        }

        public string EndTime
        {
            get => _endTime;
            set
            {
                if (SetProperty(ref _endTime, value))
                {
                    RecalculateAllStagesTimes();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasErrorMessage));
                ScrollToBottom();
            }
        }

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);
        public bool IsEditMode => _isEditMode;
        public string PageTitle => _isEditMode ? "Редактирование соревнования" : "Создание соревнования";
        public string SaveButtonText => _isEditMode ? "Сохранить изменения" : "Создать соревнование";

        // Stages properties
        public ObservableCollection<StageEditItem> Stages
        {
            get => _stages;
            set => SetProperty(ref _stages, value);
        }

        public bool HasNoStages => Stages == null || Stages.Count == 0;

        public bool ShowStageDialog
        {
            get => _showStageDialog;
            set => SetProperty(ref _showStageDialog, value);
        }

        public string StageDialogName
        {
            get => _stageDialogName;
            set => SetProperty(ref _stageDialogName, value);
        }

        public string StageDialogDescription
        {
            get => _stageDialogDescription;
            set => SetProperty(ref _stageDialogDescription, value);
        }

        public string StageDialogLocation
        {
            get => _stageDialogLocation;
            set => SetProperty(ref _stageDialogLocation, value);
        }

        public DateTime StageDialogStartDate
        {
            get => _stageDialogStartDate;
            set => SetProperty(ref _stageDialogStartDate, value);
        }

        public string StageDialogStartTime
        {
            get => _stageDialogStartTime;
            set => SetProperty(ref _stageDialogStartTime, value);
        }

        public bool IsStageStartTimeEditable
        {
            get => _isStageStartTimeEditable;
            set => SetProperty(ref _isStageStartTimeEditable, value);
        }

        public string StageDialogTitle => _isStageEditMode ? "Редактирование этапа" : "Добавление этапа";
        public string StageDialogTitleIcon => _isStageEditMode ? "✏️" : "➕";
        public string StageDialogButtonText => _isStageEditMode ? "Сохранить" : "Добавить";

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddStageCommand { get; }
        public ICommand EditStageCommand { get; }
        public ICommand DeleteStageCommand { get; }
        public ICommand SaveStageCommand { get; }
        public ICommand CancelStageDialogCommand { get; }
        public ICommand ClearErrorCommand { get; }

        public EditCompetitionViewModel()
        {
            _competitionService = new CompetitionService();

            SaveCommand = new AsyncRelayCommand(
                execute: async () => await SaveCompetitionAsync(),
                canExecute: () => !string.IsNullOrWhiteSpace(Name) &&
                                 IsValidTimeFormat(StartTime) &&
                                 IsValidTimeFormat(EndTime));
            CancelCommand = new RelayCommand(Cancel);
            AddStageCommand = new AsyncRelayCommand(ShowAddStageDialogAsync);
            EditStageCommand = new RelayCommand<StageEditItem>(ShowEditStageDialog);
            DeleteStageCommand = new RelayCommand<StageEditItem>(DeleteStage);
            SaveStageCommand = new RelayCommand(SaveStage);
            CancelStageDialogCommand = new RelayCommand(CancelStageDialog);
            ClearErrorCommand = new RelayCommand(ClearError);
        }

        private void Cancel()
        {
            _navigationService.GoBack();
        }

        private void ClearError()
        {
            ErrorMessage = "";
        }

        public void LoadCompetitionData(CompetitionDto competition)
        {
            if (competition != null)
            {
                _isEditMode = true;
                _competition = competition;
            }

            Name = competition.Name;
            Description = competition.Description;
            StartDate = competition.StartDate;
            EndDate = competition.EndDate;
            StartTime = competition.StartDate.ToString("HH:mm");
            EndTime = competition.EndDate.ToString("HH:mm");

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(SaveButtonText));

            LoadStagesAsync();
        }

        private async void LoadStagesAsync()
        {
            try
            {
                var result = await _competitionService.GetStagesAsync(_competition.Id);
                if (result.Success && result.Data != null)
                {
                    Stages.Clear();
                    foreach (var stage in result.Data.OrderBy(s => s.Order))
                    {
                        Stages.Add(new StageEditItem
                        {
                            Id = stage.Id,
                            Name = stage.Name,
                            Description = stage.Description,
                            Location = stage.Location,
                            Order = stage.Order,
                            IsFinal = stage.IsFinal,
                            StartTime = stage.StartTime,
                            EndTime = stage.EndTime,
                            IsNew = false,
                            IsDeleted = false,
                            IsStartTimeEditable = stage.Order > 1
                        });
                    }
                    RecalculateAllStagesTimes();
                    OnPropertyChanged(nameof(HasNoStages));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки расписания: {ex.Message}", "Ошибка");
            }
        }

        private bool CanAddMoreStages()
        {
            var validStages = Stages.Where(s => !s.IsDeleted).OrderBy(s => s.Order).ToList();
            if (!validStages.Any()) return true;

            var competitionEnd = CombineDateAndTime(EndDate, EndTime);
            var lastStage = validStages.Last();

            var lastStageDuration = lastStage.EndTime - lastStage.StartTime;

            // Минимальная длительность этапа 10 минут
            if (lastStageDuration <= TimeSpan.FromMinutes(10))
            {
                // Если последний этап уже минимальной длины, новый этап добавить нельзя
                ErrorMessage = "Нельзя добавить новый этап. Последний этап уже имеет минимальную длительность (10 минут).";
                return false;
            }

            // Проверяем, хватит ли места для нового этапа
            var neededTime = TimeSpan.FromMinutes(20);
            var availableTime = competitionEnd - lastStage.StartTime;

            if (availableTime < neededTime)
            {
                ErrorMessage = $"Недостаточно времени для добавления нового этапа. До конца соревнования осталось {availableTime:hh\\:mm}, необходимо минимум 20 минут (2 этапа по 10 минут).";
                return false;
            }

            ErrorMessage = "";
            return true;
        }

        // Обновите ShowAddStageDialog
        private async Task ShowAddStageDialogAsync()
        {
            // Проверяем, можно ли добавить новый этап
            if (!CanAddMoreStages())
            {
                await ShowErrorAsync(ErrorMessage);
                return;
            }

            _isStageEditMode = false;
            _editingStage = null;
            StageDialogName = "";
            StageDialogDescription = "";
            StageDialogLocation = "";

            var existingStages = Stages.Where(s => !s.IsDeleted).OrderBy(s => s.Order).ToList();
            var competitionStart = CombineDateAndTime(StartDate, StartTime);
            var competitionEnd = CombineDateAndTime(EndDate, EndTime);

            if (existingStages.Any())
            {
                var lastStage = existingStages.Last();
                var suggestedStart = lastStage.EndTime.AddHours(1);

                if (suggestedStart >= competitionEnd)
                {
                    suggestedStart = competitionEnd.AddMinutes(-10);
                }
                if (suggestedStart < lastStage.StartTime.AddMinutes(10))
                {
                    suggestedStart = lastStage.StartTime.AddMinutes(10);
                }

                StageDialogStartDate = suggestedStart.Date;
                StageDialogStartTime = suggestedStart.ToString("HH:mm");
                IsStageStartTimeEditable = true;
            }
            else
            {
                StageDialogStartDate = competitionStart.Date;
                StageDialogStartTime = competitionStart.ToString("HH:mm");
                IsStageStartTimeEditable = false;
            }

            ShowStageDialog = true;
        }

        private void RecalculateAllStagesTimes()
        {
            var validStages = Stages.Where(s => !s.IsDeleted).OrderBy(s => s.Order).ToList();
            if (!validStages.Any()) return;

            var competitionStart = CombineDateAndTime(StartDate, StartTime);
            var competitionEnd = CombineDateAndTime(EndDate, EndTime);

            if (competitionEnd <= competitionStart) return;

            // Первый этап - начало = начало соревнования
            validStages[0].StartTime = competitionStart;
            validStages[0].IsStartTimeEditable = false;

            // Для остальных этапов проверяем корректность времени начала
            for (int i = 1; i < validStages.Count; i++)
            {
                var currentStage = validStages[i];
                var previousStage = validStages[i - 1];

                // Минимальное время начала = начало предыдущего этапа + 10 минут
                var minStartTime = previousStage.StartTime.AddMinutes(10);

                if (currentStage.StartTime < minStartTime)
                {
                    currentStage.StartTime = minStartTime;
                }

                currentStage.IsStartTimeEditable = true;
            }

            // Вычисляем время окончания для каждого этапа
            for (int i = 0; i < validStages.Count; i++)
            {
                if (i == validStages.Count - 1)
                {
                    validStages[i].EndTime = competitionEnd;
                    validStages[i].IsFinal = true;
                }
                else
                {
                    validStages[i].EndTime = validStages[i + 1].StartTime;
                    validStages[i].IsFinal = false;
                }

                // Проверяем, что длительность этапа не меньше 10 минут
                var duration = validStages[i].EndTime - validStages[i].StartTime;
                if (duration < TimeSpan.FromMinutes(10))
                {
                    ErrorMessage = $"Этап \"{validStages[i].Name}\" имеет длительность {duration.Minutes} минут. Минимальная длительность этапа - 10 минут.";
                }
            }

            // Принудительное обновление коллекции для UI
            var temp = Stages.ToList();
            Stages.Clear();
            foreach (var stage in temp)
            {
                Stages.Add(stage);
            }
        }

        public void UpdateStageStartTime(StageEditItem stage, DateTime newStartTime)
        {
            if (stage == null || stage.Order == 1) return;

            var validStages = Stages.Where(s => !s.IsDeleted).OrderBy(s => s.Order).ToList();
            var stageIndex = validStages.IndexOf(stage);

            if (stageIndex < 0) return;

            var competitionEnd = CombineDateAndTime(EndDate, EndTime);
            var previousStage = validStages[stageIndex - 1];

            // Минимальное время начала = начало предыдущего этапа + 10 минут
            var minStartTime = previousStage.StartTime.AddMinutes(10);

            // Максимальное время начала
            DateTime maxStartTime;
            if (stageIndex == validStages.Count - 1)
            {
                maxStartTime = competitionEnd.AddMinutes(-10);
            }
            else
            {
                var nextStage = validStages[stageIndex + 1];
                maxStartTime = nextStage.StartTime.AddMinutes(-10);
            }

            if (newStartTime < minStartTime)
            {
                newStartTime = minStartTime;
                ErrorMessage = $"Время начала не может быть раньше {minStartTime:HH:mm} (начало предыдущего этапа + 10 минут)";
            }

            if (newStartTime > maxStartTime)
            {
                newStartTime = maxStartTime;
                ErrorMessage = stageIndex == validStages.Count - 1
                    ? $"Время начала не может быть позже {maxStartTime:HH:mm} (конец соревнования - 10 минут)"
                    : $"Время начала не может быть позже {maxStartTime:HH:mm} (начало следующего этапа - 10 минут)";
            }

            if (stage.StartTime != newStartTime)
            {
                stage.StartTime = newStartTime;
                RecalculateAllStagesTimes();
            }
        }

        private void ShowEditStageDialog(StageEditItem stage)
        {
            if (stage == null) return;

            _isStageEditMode = true;
            _editingStage = stage;
            StageDialogName = stage.Name;
            StageDialogDescription = stage.Description ?? "";
            StageDialogLocation = stage.Location ?? "";
            IsStageStartTimeEditable = stage.Order > 1;

            StageDialogStartDate = stage.StartTime.Date;
            StageDialogStartTime = stage.StartTime.ToString("HH:mm");

            ShowStageDialog = true;
        }

        private void SaveStage()
        {
            if (string.IsNullOrWhiteSpace(StageDialogName))
            {
                ErrorMessage = "Введите название этапа";
                return;
            }

            if (_isStageEditMode && _editingStage != null)
            {
                _editingStage.Name = StageDialogName.Trim();
                _editingStage.Description = StageDialogDescription?.Trim();
                _editingStage.Location = StageDialogLocation?.Trim();

                if (_editingStage.Order > 1 && IsStageStartTimeEditable)
                {
                    if (TimeSpan.TryParse(StageDialogStartTime, out var time))
                    {
                        var newStartTime = StageDialogStartDate.Date + time;
                        UpdateStageStartTime(_editingStage, newStartTime);
                    }
                }
                else
                {
                    RecalculateAllStagesTimes();
                }

                if (HasErrorMessage)
                {
                    return;
                }
            }
            else
            {
                if (!CanAddMoreStages())
                {
                    return;
                }

                var existingStages = Stages.Where(s => !s.IsDeleted).OrderBy(s => s.Order).ToList();
                int newOrder = existingStages.Any() ? existingStages.Max(s => s.Order) + 1 : 1;

                var competitionStart = CombineDateAndTime(StartDate, StartTime);
                var competitionEnd = CombineDateAndTime(EndDate, EndTime);

                DateTime newStartTime;
                bool isStartTimeEditable = newOrder > 1;

                if (newOrder == 1)
                {
                    newStartTime = competitionStart;
                }
                else
                {
                    newStartTime = StageDialogStartDate.Date + TimeSpan.Parse(StageDialogStartTime);
                    var lastStage = existingStages.Last();
                    var minStartTime = lastStage.StartTime.AddMinutes(10);

                    if (newStartTime < minStartTime)
                    {
                        newStartTime = minStartTime;
                    }
                    if (newStartTime >= competitionEnd)
                    {
                        newStartTime = competitionEnd.AddMinutes(-10);
                    }
                }

                var newStage = new StageEditItem
                {
                    Id = -Stages.Count - 1,
                    Name = StageDialogName.Trim(),
                    Description = StageDialogDescription?.Trim(),
                    Location = StageDialogLocation?.Trim(),
                    Order = newOrder,
                    IsFinal = false,
                    IsNew = true,
                    IsDeleted = false,
                    StartTime = newStartTime,
                    IsStartTimeEditable = isStartTimeEditable
                };

                Stages.Add(newStage);
                RecalculateAllStagesTimes();
            }

            ShowStageDialog = false;
            OnPropertyChanged(nameof(HasNoStages));
        }

        private void DeleteStage(StageEditItem stage)
        {
            if (stage == null) return;

            if (stage.IsNew)
            {
                Stages.Remove(stage);
            }
            else
            {
                stage.IsDeleted = true;
            }

            var remainingStages = Stages.Where(s => !s.IsDeleted).OrderBy(s => s.Order).ToList();
            for (int i = 0; i < remainingStages.Count; i++)
            {
                remainingStages[i].Order = i + 1;
            }

            RecalculateAllStagesTimes();
            OnPropertyChanged(nameof(HasNoStages));
        }

        private void CancelStageDialog()
        {
            ShowStageDialog = false;
            _editingStage = null;
            _isStageEditMode = false;
        }

        private async Task SaveCompetitionAsync()
        {
            if (!ValidateForm()) return;

            try
            {
                var startDateTime = CombineDateAndTime(StartDate, StartTime);
                var endDateTime = CombineDateAndTime(EndDate, EndTime);

                if (endDateTime <= startDateTime)
                {
                    ErrorMessage = "Дата окончания должна быть позже даты начала";
                    return;
                }

                var competitionDto = new CreateCompetitionDto
                {
                    Name = Name.Trim(),
                    Description = Description?.Trim(),
                    StartDate = startDateTime,
                    EndDate = endDateTime
                };

                var stagesToSave = Stages
                    .Where(s => !s.IsDeleted)
                    .Select(s => new StageSaveDto
                    {
                        Id = s.Id > 0 ? s.Id : (int?)null,
                        Name = s.Name,
                        Description = s.Description,
                        Location = s.Location,
                        Order = s.Order,
                        IsFinal = s.IsFinal,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    })
                    .ToList();

                ApiResponse<int> result;

                if (_isEditMode)
                {
                    result = await _competitionService.UpdateCompetitionWithStagesAsync(_competition.Id, competitionDto, stagesToSave);
                }
                else
                {
                    result = await _competitionService.CreateCompetitionWithStagesAsync(competitionDto, stagesToSave);
                }

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (result.Success)
                        await ShowSuccessAsync(result.Message);
                    else
                        await ShowErrorAsync(result.Message);

                    if (result.Success)
                    {

                        var competition = await _competitionService.GetCompetitionAsync(result.Data);
                        _navigationService.NavigateToBack(new CompetitionDetailsPage(competition.Data));
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                });
            }
        }

        private bool ValidateForm()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Название соревнования обязательно для заполнения";
                return false;
            }

            if (!IsValidTimeFormat(StartTime))
            {
                ErrorMessage = "Неверный формат времени начала (используйте HH:mm)";
                return false;
            }

            if (!IsValidTimeFormat(EndTime))
            {
                ErrorMessage = "Неверный формат времени окончания (используйте HH:mm)";
                return false;
            }

            return true;
        }

        private DateTime CombineDateAndTime(DateTime date, string time)
        {
            if (TimeSpan.TryParse(time, out var timeSpan))
            {
                return date.Date + timeSpan;
            }
            return date;
        }

        private bool IsValidTimeFormat(string time)
        {
            return TimeSpan.TryParse(time, out _);
        }

        private void ScrollToBottom()
        {
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ScrollToBottomRequested;

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            Name = null;
            Description = null;
            ErrorMessage = null;
            StartTime = null;
            EndTime = null;
            Stages?.Clear();

            if (_competitionService is IDisposable disposable)
                disposable.Dispose();
        }
    }

    public class StageEditItem : BaseViewModel
    {
        private int _id;
        private string _name;
        private string _description;
        private string _location;
        private int _order;
        private bool _isFinal;
        private DateTime _startTime;
        private DateTime _endTime;
        private bool _isNew;
        private bool _isDeleted;
        private bool _isStartTimeEditable;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public int Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        public bool IsFinal
        {
            get => _isFinal;
            set => SetProperty(ref _isFinal, value);
        }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (SetProperty(ref _startTime, value))
                {
                    OnPropertyChanged(nameof(TimeRangeText));
                }
            }
        }

        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (SetProperty(ref _endTime, value))
                {
                    OnPropertyChanged(nameof(TimeRangeText));
                }
            }
        }

        public bool IsNew
        {
            get => _isNew;
            set => SetProperty(ref _isNew, value);
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set => SetProperty(ref _isDeleted, value);
        }

        public bool IsStartTimeEditable
        {
            get => _isStartTimeEditable;
            set => SetProperty(ref _isStartTimeEditable, value);
        }

        public string TimeRangeText => $"{StartTime:dd.MM.yyyy HH:mm} - {EndTime:dd.MM.yyyy HH:mm}";
    }
}