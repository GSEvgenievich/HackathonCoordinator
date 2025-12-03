using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class EditTaskViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly TeamService _teamService;
        private readonly TaskService _taskService;

        private string _title = "";
        public string Title
        {
            get => _title;

            set
            {
                SetProperty(ref _title, value);
                ((AsyncRelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
            }

        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private TaskTypeDto _selectedType;
        public TaskTypeDto SelectedType
        {
            get => _selectedType;
            set
            {
                SetProperty(ref _selectedType, value);
                UpdateGitHubBranchHint();
                ((AsyncRelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
            }
        }

        private MemberDto _selectedAssignee;
        public MemberDto SelectedAssignee
        {
            get => _selectedAssignee;
            set => SetProperty(ref _selectedAssignee, value);
        }

        private DateTime? _deadlineDate;
        public DateTime? DeadlineDate
        {
            get => _deadlineDate;
            set
            {
                SetProperty(ref _deadlineDate, value);
                UpdateFullDeadline();
                ((AsyncRelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _deadlineTime = "23:59";
        public string DeadlineTime
        {
            get => _deadlineTime;
            set
            {
                SetProperty(ref _deadlineTime, value);
                UpdateFullDeadline();
                ((AsyncRelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
            }
        }

        private DateTime? _deadline;
        public DateTime? Deadline
        {
            get => _deadline;
            set => SetProperty(ref _deadline, value);
        }

        private string _githubBranchName = "";
        public string GitHubBranchName
        {
            get => _githubBranchName;
            set
            {
                SetProperty(ref _githubBranchName, value);
                ((AsyncRelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _githubBranchHint = "feature/";
        public string GithubBranchHint
        {
            get => _githubBranchHint;
            set => SetProperty(ref _githubBranchHint, value);
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                SetProperty(ref _isEditMode, value);
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }

        private bool _hasGitHubRepo = false;
        public bool HasGitHubRepo
        {
            get => _hasGitHubRepo;
            set
            {
                SetProperty(ref _hasGitHubRepo, value);
                OnPropertyChanged(nameof(ShowGitHubSection));
                ((AsyncRelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
            }
        }

        private bool _isBranchReadOnly = false;
        public bool IsBranchReadOnly
        {
            get => _isBranchReadOnly;
            set => SetProperty(ref _isBranchReadOnly, value);
        }

        private string _branchToolTip = "";
        public string BranchToolTip
        {
            get => _branchToolTip;
            set => SetProperty(ref _branchToolTip, value);
        }

        private bool _hasExistingBranch = false;
        public bool HasExistingBranch
        {
            get => _hasExistingBranch;
            set
            {
                SetProperty(ref _hasExistingBranch, value);
                OnPropertyChanged(nameof(ShowBranchWarning));
                OnPropertyChanged(nameof(ShowBranchInfo));
                UpdateBranchReadOnlyState();
            }
        }

        public bool ShowGitHubSection => HasGitHubRepo;
        public bool ShowBranchWarning => HasExistingBranch && IsEditMode;
        public bool ShowBranchInfo => !HasExistingBranch && !string.IsNullOrEmpty(GitHubBranchName) && HasGitHubRepo;

        public int TaskId { get; private set; }
        public int ProjectId { get; private set; }

        public ObservableCollection<TaskTypeDto> TaskTypes { get; set; } = new();
        public ObservableCollection<MemberDto> TeamMembers { get; set; } = new();

        public string PageTitle => IsEditMode ? "Редактирование задачи" : "Создание задачи";
        public string PageSubtitle => IsEditMode ? "Редактирование существующей задачи" : "Создание новой задачи для команды";
        public string SaveButtonText => IsEditMode ? "Сохранить изменения" : "Создать задачу";

        // AsyncRelayCommand для сохранения задачи
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditTaskViewModel()
        {
            _navigationService = App.NavigationService;
            _teamService = new TeamService();
            _taskService = new TaskService();

            SaveCommand = new AsyncRelayCommand(
                execute: async () => await SaveTaskAsync(),
                canExecute: () => !string.IsNullOrWhiteSpace(Title) &&
                                 SelectedType != null);

            CancelCommand = new RelayCommand(() => Cancel());
        }

        /// <summary>
        /// Инициализация ViewModel для создания новой задачи
        /// </summary>
        /// <param name="projectId">ID проекта (команды)</param>
        public async void InitializeForCreate(int projectId)
        {
            try
            {
                ProjectId = projectId;
                IsEditMode = false;

                await LoadTeamDataAsync();
                await LoadTaskTypesAsync();
                await LoadTeamMembersAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Устанавливаем дефолтные значения
                    DeadlineDate = DateTime.Today.AddDays(7);
                    DeadlineTime = "23:59";

                    if (TaskTypes.Any())
                        SelectedType = TaskTypes.First();

                    IsBranchReadOnly = false;
                    BranchToolTip = "Введите название для новой ветки GitHub";

                    ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Ошибка инициализации: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// Инициализация ViewModel для редактирования существующей задачи
        /// </summary>
        /// <param name="taskId">ID задачи для редактирования</param>
        public async void InitializeForEdit(int taskId)
        {
            try
            {
                TaskId = taskId;
                IsEditMode = true;

                await LoadTeamDataAsync();
                await LoadTaskTypesAsync();
                await LoadTeamMembersAsync();
                await LoadTaskDataAsync(taskId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Ошибка инициализации: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// Загрузка данных команды для определения наличия GitHub репозитория
        /// </summary>
        private async Task LoadTeamDataAsync()
        {
            try
            {
                var team = await _teamService.GetCurrentTeamAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (team.Success)
                    {
                        HasGitHubRepo = !string.IsNullOrEmpty(team.Data.GitHubUrl) && team.Data.GitHubUrl != "Не указан";
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Ошибка загрузки данных команды: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// Загрузка доступных типов задач из сервиса
        /// </summary>
        private async Task LoadTaskTypesAsync()
        {
            try
            {
                var types = await _taskService.GetTaskTypesAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TaskTypes.Clear();
                    foreach (var type in types.Data)
                    {
                        TaskTypes.Add(type);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Ошибка загрузки типов задач: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// Загрузка участников команды для назначения задачи
        /// </summary>
        private async Task LoadTeamMembersAsync()
        {
            try
            {
                var team = await _teamService.GetCurrentTeamAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (team.Success)
                    {
                        TeamMembers.Clear();

                        foreach (var member in team.Data.Members)
                        {
                            TeamMembers.Add(member);
                        }

                        if (!TeamMembers.Any())
                        {
                            ErrorMessage = "В команде нет участников, которым можно назначить задачу";
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Ошибка загрузки участников команды: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// Загрузка данных существующей задачи для редактирования
        /// </summary>
        /// <param name="taskId">ID задачи</param>
        private async Task LoadTaskDataAsync(int taskId)
        {
            try
            {
                var task = await _taskService.GetTaskDetailsAsync(taskId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (task.Success)
                    {
                        Title = task.Data.Title;
                        Description = task.Data.Description ?? "";
                        SelectedType = TaskTypes.FirstOrDefault(t => t.Id == task.Data.TypeId);
                        SelectedAssignee = TeamMembers.FirstOrDefault(m => m.Id == task.Data.AssignedToId);

                        if (task.Data.Deadline.HasValue)
                        {
                            DeadlineDate = task.Data.Deadline.Value.Date;
                            DeadlineTime = task.Data.Deadline.Value.ToString("HH:mm");
                        }
                        else
                        {
                            DeadlineDate = DateTime.Today.AddDays(7);
                            DeadlineTime = "23:59";
                        }

                        GitHubBranchName = task.Data.GitHubBranchName ?? "";

                        HasExistingBranch = !string.IsNullOrEmpty(task.Data.GitHubBranchName);

                        UpdateGitHubBranchHint();
                        UpdateBranchReadOnlyState();
                    }
                    else
                    {
                        ErrorMessage = $"Ошибка загрузки задачи: {task.Message}";
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Ошибка загрузки задачи: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// Обновление полного дедлайна на основе даты и времени
        /// </summary>
        private void UpdateFullDeadline()
        {
            if (!DeadlineDate.HasValue)
            {
                Deadline = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(DeadlineTime) || !TimeSpan.TryParse(DeadlineTime, out var time))
            {
                time = new TimeSpan(23, 59, 0);
            }

            Deadline = DeadlineDate.Value.Date.Add(time);
        }

        /// <summary>
        /// Обновление подсказки для названия ветки в зависимости от типа задачи
        /// </summary>
        private void UpdateGitHubBranchHint()
        {
            if (SelectedType == null) return;

            switch (SelectedType.Name.ToLower())
            {
                case "баг":
                case "bug":
                    GithubBranchHint = "bugfix/";
                    break;
                case "документация":
                case "documentation":
                    GithubBranchHint = "docs/";
                    break;
                case "фича":
                case "feature":
                default:
                    GithubBranchHint = "feature/";
                    break;
            }
        }

        /// <summary>
        /// Обновление состояния поля ввода ветки (только для чтения или редактируемое)
        /// </summary>
        private void UpdateBranchReadOnlyState()
        {
            if (IsEditMode && HasExistingBranch)
            {
                IsBranchReadOnly = true;
                BranchToolTip = "Ветка уже создана в GitHub. Изменить название нельзя.";
            }
            else if (HasGitHubRepo)
            {
                IsBranchReadOnly = false;
                BranchToolTip = "Введите название для новой ветки GitHub";
            }
            else
            {
                IsBranchReadOnly = true;
                BranchToolTip = "Для создания ветки необходимо подключить GitHub репозиторий к команде";
            }
        }

        /// <summary>
        /// Сохранение задачи (создание новой или обновление существующей)
        /// </summary>
        private async Task SaveTaskAsync()
        {
            if (!ValidateForm())
                return;

            try
            {
                var dto = new CreateTaskDto
                {
                    Title = Title.Trim(),
                    Description = Description?.Trim(),
                    TypeId = SelectedType?.Id ?? 1,
                    AssignedToId = SelectedAssignee?.Id,
                    Deadline = Deadline,
                    GitHubBranchName = HasGitHubRepo && !string.IsNullOrWhiteSpace(GitHubBranchName)
                        ? GitHubBranchName.Trim()
                        : null
                };

                if (IsEditMode)
                {
                    await SaveExistingTaskAsync(dto);
                }
                else
                {
                    await CreateNewTaskAsync(dto);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// Сохранение изменений существующей задачи
        /// </summary>
        /// <param name="dto">DTO с данными задачи</param>
        private async Task SaveExistingTaskAsync(CreateTaskDto dto)
        {
            try
            {
                var result = await _taskService.UpdateTaskAsync(TaskId, dto);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        var message = result.Message;

                        if (!string.IsNullOrWhiteSpace(dto.GitHubBranchName) && !HasExistingBranch)
                        {
                            if (!message.Contains("ветк"))
                            {
                                message += $"\n\n✅ Ветка '{GitHubBranchName}' создана в GitHub репозитории";
                            }
                        }

                        MessageBox.Show(message, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        _navigationService.NavigateTo(new TeamPage());
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка сохранения задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// Создание новой задачи
        /// </summary>
        /// <param name="dto">DTO с данными задачи</param>
        private async Task CreateNewTaskAsync(CreateTaskDto dto)
        {
            try
            {
                var result = await _taskService.CreateTaskAsync(ProjectId, dto);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        var message = result.Message;

                        if (!string.IsNullOrWhiteSpace(dto.GitHubBranchName))
                        {
                            if (!message.Contains("ветк"))
                            {
                                message += $"\n\nВетка '{GitHubBranchName}' создана в GitHub репозитории команды";
                            }
                        }

                        MessageBox.Show(message, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        _navigationService.NavigateTo(new TeamPage());
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка создания задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// Отмена редактирования и возврат на предыдущую страницу
        /// </summary>
        private void Cancel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _navigationService.NavigateTo(new TeamPage());
            });
        }

        /// <summary>
        /// Валидация формы перед сохранением
        /// </summary>
        /// <returns>True если форма валидна, иначе False</returns>
        private bool ValidateForm()
        {
            string errorMessage = "";

            if (string.IsNullOrWhiteSpace(Title))
            {
                errorMessage = "Название задачи обязательно для заполнения";
            }
            else if (SelectedType == null)
            {
                errorMessage = "Выберите тип задачи";
            }
            else if (Title.Length > 200)
            {
                errorMessage = "Название задачи не должно превышать 200 символов";
            }
            else if (Description?.Length > 1000)
            {
                errorMessage = "Описание задачи не должно превышать 1000 символов";
            }
            else if (DeadlineDate.HasValue)
            {
                if (!IsValidTimeFormat(DeadlineTime))
                {
                    errorMessage = "Время должно быть в формате HH:mm (например: 14:30)";
                }
                // Проверка что дедлайн не в прошлом
                else if (Deadline.HasValue && Deadline.Value < DateTime.Now)
                {
                    errorMessage = "Дедлайн не может быть в прошлом";
                }
            }
            else if (HasGitHubRepo && !string.IsNullOrWhiteSpace(GitHubBranchName))
            {
                if (!IsValidBranchName(GitHubBranchName))
                {
                    errorMessage = "Название ветки может содержать только буквы, цифры, дефисы, подчеркивания и слеши";
                }
                else if (GitHubBranchName.StartsWith("/") || GitHubBranchName.EndsWith("/"))
                {
                    errorMessage = "Название ветки не может начинаться или заканчиваться слешем";
                }
            }

            // Обновляем свойство ErrorMessage через Dispatcher, если нужно
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorMessage = errorMessage;
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ErrorMessage = "";
                });
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        /// <summary>
        /// Проверка валидности формата времени
        /// </summary>
        private bool IsValidTimeFormat(string time)
        {
            if (string.IsNullOrWhiteSpace(time))
                return false;

            return Regex.IsMatch(time, @"^([01]?[0-9]|2[0-3]):[0-5][0-9]$");
        }

        /// <summary>
        /// Проверка валидности имени ветки GitHub
        /// </summary>
        /// <param name="branchName">Имя ветки для проверки</param>
        /// <returns>True если имя валидно, иначе False</returns>
        private bool IsValidBranchName(string branchName)
        {
            return Regex.IsMatch(branchName, @"^[a-zA-Z0-9._\/-]+$");
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            Application.Current.Dispatcher.Invoke(() =>
            {
                TaskTypes?.Clear();
                TeamMembers?.Clear();

                Title = null;
                Description = null;
                GitHubBranchName = null;
                GithubBranchHint = null;
                ErrorMessage = null;
                BranchToolTip = null;

                SelectedType = null;
                SelectedAssignee = null;
            });

            if (_teamService is IDisposable teamDisposable)
                teamDisposable.Dispose();

            if (_taskService is IDisposable taskDisposable)
                taskDisposable.Dispose();
        }
    }
}