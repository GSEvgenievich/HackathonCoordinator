using HackathonCoordinator.ServiceLayer;
using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class EditTaskViewModel : BaseViewModel
    {
        public bool doDispose = true;
        private bool _isInitialized = false;

        private readonly TaskService _taskService;
        private readonly TeamService _teamService;

        private int _teamId;
        private int? _taskId;
        private bool _isCreateMode;

        // Поля задачи
        private string _title = "";
        private string _description = "";
        private int _selectedTypeId = 1;
        private int? _selectedAssigneeId;
        private DateTime? _deadline;
        private string _gitHubBranchName = "";
        private string _errorMessage = "";

        // Коллекции
        private ObservableCollection<TaskTypeDto> _taskTypes = new();
        private ObservableCollection<MemberDto> _teamMembers = new();

        // Выбранные элементы
        private TaskTypeDto _selectedType;
        private MemberDto _selectedAssignee;

        // Свойства для UI (DatePicker и Time)
        private DateTime? _deadlineDate;
        private string _deadlineTime = "";

        public DateTime? DeadlineDate
        {
            get => _deadlineDate;
            set
            {
                if (SetProperty(ref _deadlineDate, value))
                {
                    UpdateDeadline();
                }
            }
        }

        public string DeadlineTime
        {
            get => _deadlineTime;
            set
            {
                if (SetProperty(ref _deadlineTime, value))
                {
                    UpdateDeadline();
                }
            }
        }

        private void UpdateDeadline()
        {
            if (_deadlineDate.HasValue)
            {
                if (TimeSpan.TryParse(_deadlineTime, out var time))
                {
                    Deadline = _deadlineDate.Value.Date + time;
                }
                else
                {
                    Deadline = _deadlineDate.Value.Date;
                }
            }
            else
            {
                Deadline = null;
            }
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public int SelectedTypeId
        {
            get => _selectedTypeId;
            set => SetProperty(ref _selectedTypeId, value);
        }

        public int? SelectedAssigneeId
        {
            get => _selectedAssigneeId;
            set => SetProperty(ref _selectedAssigneeId, value);
        }

        public DateTime? Deadline
        {
            get => _deadline;
            set => SetProperty(ref _deadline, value);
        }

        public string GitHubBranchName
        {
            get => _gitHubBranchName;
            set => SetProperty(ref _gitHubBranchName, value);
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

        public ObservableCollection<TaskTypeDto> TaskTypes
        {
            get => _taskTypes;
            set => SetProperty(ref _taskTypes, value);
        }

        public ObservableCollection<MemberDto> TeamMembers
        {
            get => _teamMembers;
            set => SetProperty(ref _teamMembers, value);
        }

        public TaskTypeDto SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value))
                {
                    SelectedTypeId = value?.Id ?? 0;
                }
            }
        }

        public MemberDto SelectedAssignee
        {
            get => _selectedAssignee;
            set
            {
                if (SetProperty(ref _selectedAssignee, value))
                {
                    SelectedAssigneeId = value?.Id;
                }
            }
        }

        public string PageTitle => _isCreateMode ? "Создание задачи" : "Редактирование задачи";
        public string PageSubtitle => _isCreateMode ? "Новая задача для команды" : "Изменение существующей задачи";
        public string SaveButtonText => _isCreateMode ? "Создать задачу" : "Сохранить изменения";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditTaskViewModel()
        {
            _taskService = new TaskService();
            _teamService = new TeamService();

            SaveCommand = new AsyncRelayCommand(SaveTaskAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        public async Task InitializeForCreateAsync(int teamId)
        {
            _isCreateMode = true;
            _teamId = teamId;
            IsLoading = true;

            await LoadTaskTypesAsync();
            await LoadTeamMembersAsync();

            // Устанавливаем дефолтные значения
            if (TaskTypes.Any())
            {
                SelectedType = TaskTypes[0];
            }

            // Устанавливаем дефолтное время
            DeadlineTime = "23:59";

            IsLoading = false;
            _isInitialized = true;
        }

        public async Task InitializeForEditAsync(TaskDetailsDto task)
        {
            if (task == null)
            {
                await ShowErrorAsync("Задача не найдена");
                Cancel();
                return;
            }

            _isCreateMode = false;
            _taskId = task.Id;
            _teamId = task.TeamId;
            IsLoading = true;

            Title = task.Title;
            Description = task.Description ?? "";
            SelectedTypeId = task.TypeId;
            SelectedAssigneeId = task.AssignedToId;
            Deadline = task.Deadline;
            GitHubBranchName = task.GitHubBranchName ?? "";

            // Устанавливаем значения для DatePicker и Time
            if (Deadline.HasValue)
            {
                var date = Deadline.Value.Date;
                var time = Deadline.Value.ToString("HH:mm");

                _deadlineDate = date;
                _deadlineTime = time;

                OnPropertyChanged(nameof(DeadlineDate));
                OnPropertyChanged(nameof(DeadlineTime));

                // Явно обновляем составной дедлайн
                if (TimeSpan.TryParse(time, out var timeSpan))
                {
                    Deadline = date + timeSpan;
                }
            }
            else
            {
                _deadlineDate = null;
                _deadlineTime = "23:59";

                OnPropertyChanged(nameof(DeadlineDate));
                OnPropertyChanged(nameof(DeadlineTime));
                Deadline = null;
            }

            await LoadTaskTypesAsync();
            await LoadTeamMembersAsync();

            // Устанавливаем SelectedType после загрузки TaskTypes
            SelectedType = TaskTypes.FirstOrDefault(t => t.Id == SelectedTypeId);

            // Устанавливаем SelectedAssignee после загрузки TeamMembers
            SelectedAssignee = TeamMembers.FirstOrDefault(m => m.Id == SelectedAssigneeId);

            IsLoading = false;
            _isInitialized = true;
        }

        private async Task LoadTaskTypesAsync()
        {
            try
            {
                var types = await _taskService.GetTaskTypesAsync();
                if (types.Success && types.Data != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        TaskTypes.Clear();
                        foreach (var type in types.Data)
                        {
                            TaskTypes.Add(type);
                        }
                    });
                }
                else
                {
                    await ShowErrorAsync($"Ошибка загрузки типов задач: {types.Message}");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки типов задач: {ex.Message}");
            }
        }

        private async Task LoadTeamMembersAsync()
        {
            try
            {
                var team = await _teamService.GetTeamByIdAsync(_teamId);
                if (team.Success && team.Data?.Members != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        TeamMembers.Clear();
                        foreach (var member in team.Data.Members)
                        {
                            TeamMembers.Add(member);
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Не удалось загрузить участников команды: {team.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки участников команды: {ex.Message}");
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title) && SelectedTypeId > 0;
        }

        private async Task SaveTaskAsync()
        {
            if (!CanSave()) return;

            IsLoading = true;

            try
            {
                var dto = new CreateTaskDto
                {
                    Title = Title.Trim(),
                    Description = Description?.Trim(),
                    TypeId = SelectedTypeId,
                    AssignedToId = SelectedAssigneeId,
                    Deadline = Deadline,
                    GitHubBranchName = GitHubBranchName?.Trim()
                };

                ApiResponse result;

                if (_isCreateMode)
                {
                    result = await _taskService.CreateTaskAsync(_teamId, dto);
                }
                else
                {
                    result = await _taskService.UpdateTaskAsync(_taskId.Value, dto);
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        MessageBox.Show(result.Message, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        Cancel();
                    }
                    else
                    {
                        ErrorMessage = result.Message;
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка сохранения: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            _navigationService.GoBack();
        }

        private void ScrollToBottom()
        {
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ScrollToBottomRequested;

        protected override void DisposeManagedResources()
        {
            if (!doDispose) return;
            base.DisposeManagedResources();

            TaskTypes?.Clear();
            TeamMembers?.Clear();

            if (_taskService is IDisposable taskDisposable) taskDisposable.Dispose();
            if (_teamService is IDisposable teamDisposable) teamDisposable.Dispose();
        }
    }
}