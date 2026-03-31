// HackathonCoordinator.WPFClient/ViewModels/UsersManagementViewModel.cs
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
    public class UsersManagementViewModel : BaseViewModel
    {
        public bool doDispose = true;

        private readonly UserService _userService;
        private readonly PositionService _positionService;
        private readonly NavigationService _navigationService;

        private ObservableCollection<UserDto> _allUsers = new();
        private ObservableCollection<PositionDto> _allPositions = new();
        private ObservableCollection<UserDto> _filteredUsers = new();

        private string _searchText = "";
        private UserDto _userToDelete;
        private UserDto _userToManage;
        private bool _showDeleteDialog;
        private bool _showManageRoleDialog;
        private bool _showPositionsPanel;
        private PositionDto _selectedPosition;
        private string _newPositionName;
        private string _editPositionName;
        private PositionDto _positionToEdit;
        private string _errorMessage;

        public ObservableCollection<UserDto> AllUsers
        {
            get => _allUsers;
            set
            {
                SetProperty(ref _allUsers, value);
                FilterUsers();
                UpdateStatistics();
            }
        }

        public ObservableCollection<PositionDto> AllPositions
        {
            get => _allPositions;
            set => SetProperty(ref _allPositions, value);
        }

        public ObservableCollection<UserDto> FilteredUsers
        {
            get => _filteredUsers;
            set => SetProperty(ref _filteredUsers, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterUsers();
            }
        }

        public UserDto UserToDelete
        {
            get => _userToDelete;
            set => SetProperty(ref _userToDelete, value);
        }

        public UserDto UserToManage
        {
            get => _userToManage;
            set
            {
                SetProperty(ref _userToManage, value);
                OnPropertyChanged(nameof(IsUserOrganizer));
                OnPropertyChanged(nameof(IsUserNotOrganizer));
                OnPropertyChanged(nameof(RoleManageMessage));
            }
        }

        public bool ShowDeleteDialog
        {
            get => _showDeleteDialog;
            set => SetProperty(ref _showDeleteDialog, value);
        }

        public bool ShowManageRoleDialog
        {
            get => _showManageRoleDialog;
            set => SetProperty(ref _showManageRoleDialog, value);
        }

        public bool ShowPositionsPanel
        {
            get => _showPositionsPanel;
            set => SetProperty(ref _showPositionsPanel, value);
        }

        public PositionDto SelectedPosition
        {
            get => _selectedPosition;
            set => SetProperty(ref _selectedPosition, value);
        }

        public string NewPositionName
        {
            get => _newPositionName;
            set => SetProperty(ref _newPositionName, value);
        }

        public string EditPositionName
        {
            get => _editPositionName;
            set => SetProperty(ref _editPositionName, value);
        }

        public PositionDto PositionToEdit
        {
            get => _positionToEdit;
            set
            {
                SetProperty(ref _positionToEdit, value);
                if (value != null)
                {
                    EditPositionName = value.Name;
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public int TotalUsersCount => AllUsers?.Count ?? 0;
        public int UsersInTeamsCount => AllUsers?.Count(u => u.TeamId.HasValue) ?? 0;
        public int UsersWithoutTeamCount => AllUsers?.Count(u => !u.TeamId.HasValue) ?? 0;
        public bool HasNoUsers => !(FilteredUsers?.Any() ?? false);
        public bool IsUserOrganizer => UserToManage?.RoleId == (int)Roles.Organizer;
        public bool IsUserNotOrganizer => UserToManage != null && UserToManage.RoleId != (int)Roles.Organizer;

        public string RoleManageMessage
        {
            get
            {
                if (UserToManage == null) return "";

                if (UserToManage.RoleId == (int)Roles.Organizer)
                {
                    return "Вы уверены, что хотите снять права организатора с этого пользователя?";
                }
                else if (UserToManage.RoleId == (int)Roles.Member || UserToManage.RoleId == (int)Roles.Captain)
                {
                    return "Вы уверены, что хотите назначить этого пользователя организатором?";
                }
                else
                {
                    return "Нельзя изменить роль администратора";
                }
            }
        }

        // Определяем роль текущего пользователя
        public bool IsAdmin { get; private set; }
        public bool IsOrganizer { get; private set; }

        public ObservableCollection<UserListStatusFilter> StatusFilters { get; } = new()
        {
            new UserListStatusFilter { Id = 0, Name = "Все участники" },
            new UserListStatusFilter { Id = 1, Name = "В командах" },
            new UserListStatusFilter { Id = 2, Name = "Без команды" },
            new UserListStatusFilter { Id = 3, Name = "С GitHub" },
            new UserListStatusFilter { Id = 4, Name = "Капитаны" },
            new UserListStatusFilter { Id = 5, Name = "Организаторы" }
        };

        private UserListStatusFilter _selectedStatusFilter;
        public UserListStatusFilter SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                SetProperty(ref _selectedStatusFilter, value);
                FilterUsers();
            }
        }

        // Команды
        public ICommand BackCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ViewUserCommand { get; }
        public ICommand ManageRoleCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }
        public ICommand ConfirmMakeOrganizerCommand { get; }
        public ICommand ConfirmRemoveOrganizerCommand { get; }
        public ICommand CancelManageRoleCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand TogglePositionsPanelCommand { get; }
        public ICommand CreatePositionCommand { get; }
        public ICommand EditPositionCommand { get; }
        public ICommand DeletePositionCommand { get; }

        public UsersManagementViewModel()
        {
            _userService = new UserService();
            _positionService = new PositionService();
            _navigationService = App.NavigationService;

            // Инициализация команд
            BackCommand = new RelayCommand(GoBack);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            CancelDeleteCommand = new RelayCommand(CancelDelete);
            CancelManageRoleCommand = new RelayCommand(CancelManageRole);
            TogglePositionsPanelCommand = new RelayCommand(TogglePositionsPanel);

            DeleteUserCommand = new AsyncRelayCommand<UserDto>(
                execute: async (user) => await DeleteUserAsync(user),
                canExecute: (user) => user != null && (IsAdmin || (IsOrganizer && user.RoleId == (int)Roles.Member)));

            ViewUserCommand = new AsyncRelayCommand<UserDto>(
                execute: async (user) => await ExecuteViewUserAsync(user),
                canExecute: (user) => user != null);

            ManageRoleCommand = new AsyncRelayCommand<UserDto>(
                execute: async (user) => await ShowManageRoleDialogAsync(user),
                canExecute: (user) => user != null && IsAdmin && user.RoleId != (int)Roles.Admin);

            ConfirmDeleteCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteDeleteUserAsync(),
                canExecute: () => UserToDelete != null);

            ConfirmMakeOrganizerCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteMakeOrganizerAsync(),
                canExecute: () => UserToManage != null);

            ConfirmRemoveOrganizerCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteRemoveOrganizerAsync(),
                canExecute: () => UserToManage != null);

            RefreshCommand = new AsyncRelayCommand(
                execute: async () => await LoadAllDataAsync(),
                canExecute: () => true);

            CreatePositionCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteCreatePositionAsync(),
                canExecute: () => IsAdmin && !string.IsNullOrWhiteSpace(NewPositionName));

            EditPositionCommand = new AsyncRelayCommand<PositionDto>(
                execute: async (position) => await ExecuteEditPositionAsync(position),
                canExecute: (position) => IsAdmin && position != null && !position.IsProtected);

            DeletePositionCommand = new AsyncRelayCommand<PositionDto>(
                execute: async (position) => await ExecuteDeletePositionAsync(position),
                canExecute: (position) => IsAdmin && position != null && !position.IsProtected);

            SelectedStatusFilter = StatusFilters[0];

            // Загружаем данные
            LoadAllDataAsync();
            CheckUserRole();
        }

        private async void CheckUserRole()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user.Success)
                {
                    IsAdmin = user.Data.RoleId == (int)Roles.Admin;
                    IsOrganizer = user.Data.RoleId == (int)Roles.Organizer;

                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(IsOrganizer));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка проверки роли: {ex.Message}");
            }
        }

        private async Task LoadAllDataAsync()
        {
            await LoadUsersAsync();
            await LoadPositionsAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var result = await _userService.GetAllUsersAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        AllUsers = new ObservableCollection<UserDto>(result.Data.Where(u => u.RoleId != (int)Roles.Admin));
                    }
                    else
                    {
                        ShowError(result.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        private async Task LoadPositionsAsync()
        {
            try
            {
                var result = await _positionService.GetAllPositionsAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        AllPositions = new ObservableCollection<PositionDto>(result.Data);
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки должностей: {ex.Message}");
            }
        }

        private async Task DeleteUserAsync(UserDto user)
        {
            if (user == null) return;

            UserToDelete = user;
            ShowDeleteDialog = true;
        }

        private async Task ShowManageRoleDialogAsync(UserDto user)
        {
            UserToManage = user;
            ShowManageRoleDialog = true;
        }

        private void FilterUsers()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (AllUsers == null) return;

                var filtered = AllUsers.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(u =>
                        u.Username.ToLower().Contains(searchLower) ||
                        u.Email.ToLower().Contains(searchLower));
                }

                if (SelectedStatusFilter != null)
                {
                    filtered = SelectedStatusFilter.Id switch
                    {
                        1 => filtered.Where(u => u.TeamId.HasValue),
                        2 => filtered.Where(u => !u.TeamId.HasValue),
                        3 => filtered.Where(u => !string.IsNullOrEmpty(u.GitHubUsername)),
                        4 => filtered.Where(u => u.RoleId == (int)Roles.Captain),
                        5 => filtered.Where(u => u.RoleId == (int)Roles.Organizer),
                        _ => filtered
                    };
                }

                FilteredUsers = new ObservableCollection<UserDto>(filtered);
                OnPropertyChanged(nameof(HasNoUsers));
            });
        }

        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalUsersCount));
            OnPropertyChanged(nameof(UsersInTeamsCount));
            OnPropertyChanged(nameof(UsersWithoutTeamCount));
        }

        private void ResetFilters()
        {
            SearchText = "";
            SelectedStatusFilter = StatusFilters[0];
        }

        private void CancelDelete()
        {
            ShowDeleteDialog = false;
            UserToDelete = null;
        }

        private void CancelManageRole()
        {
            ShowManageRoleDialog = false;
            UserToManage = null;
        }

        private async Task ExecuteDeleteUserAsync()
        {
            if (UserToDelete == null) return;

            try
            {
                var result = await _userService.DeleteUserAsync(UserToDelete.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(result.Message,
                        result.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (result.Success)
                    {
                        AllUsers.Remove(UserToDelete);
                        FilterUsers();
                        UpdateStatistics();
                    }

                    ShowDeleteDialog = false;
                    UserToDelete = null;
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка удаления пользователя: {ex.Message}");
            }
        }

        private async Task ExecuteMakeOrganizerAsync()
        {
            if (UserToManage == null) return;

            try
            {
                var result = await _userService.MakeOrganizerAsync(UserToManage.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(result.Message,
                        result.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (result.Success)
                    {
                        UserToManage.RoleId = (int)Roles.Organizer;
                        UserToManage.RoleName = "Организатор";
                        FilterUsers();
                    }

                    ShowManageRoleDialog = false;
                    UserToManage = null;
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка назначения организатора: {ex.Message}");
            }
        }

        private async Task ExecuteRemoveOrganizerAsync()
        {
            if (UserToManage == null) return;

            try
            {
                var result = await _userService.RemoveOrganizerAsync(UserToManage.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(result.Message,
                        result.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (result.Success)
                    {
                        UserToManage.RoleId = (int)Roles.Member;
                        UserToManage.RoleName = "Участник";
                        FilterUsers();
                    }

                    ShowManageRoleDialog = false;
                    UserToManage = null;
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка снятия прав организатора: {ex.Message}");
            }
        }

        private async Task ExecuteViewUserAsync(UserDto user)
        {
            if (user == null) return;

            try
            {
                doDispose = false;
                var profilePage = new ProfilePage(user.Id);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _navigationService.NavigateTo(profilePage);
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка открытия профиля: {ex.Message}");
                doDispose = true;
            }
        }

        private void GoBack()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                    {
                        mainViewModel.OpenMainPage();
                    }
                }
            });
        }

        private void TogglePositionsPanel()
        {
            ShowPositionsPanel = !ShowPositionsPanel;
            if (ShowPositionsPanel)
            {
                LoadPositionsAsync();
            }
        }

        private async Task ExecuteCreatePositionAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPositionName)) return;

            try
            {
                var result = await _positionService.CreatePositionAsync(NewPositionName.Trim());

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        AllPositions.Add(result.Data);
                        NewPositionName = "";
                        MessageBox.Show("Должность успешно создана!", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка создания должности: {ex.Message}");
            }
        }

        private async Task ExecuteEditPositionAsync(PositionDto position)
        {
            if (position == null || position.IsProtected) return;

            PositionToEdit = position;

            var newName = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return Microsoft.VisualBasic.Interaction.InputBox(
                    "Введите новое название должности:",
                    "Редактирование должности",
                    position.Name);
            });

            if (!string.IsNullOrWhiteSpace(newName) && newName != position.Name)
            {
                var result = await _positionService.UpdatePositionAsync(position.Id, newName.Trim());

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        position.Name = newName.Trim();
                        MessageBox.Show("Должность успешно обновлена!", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(result.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }

            PositionToEdit = null;
        }

        private async Task ExecuteDeletePositionAsync(PositionDto position)
        {
            if (position == null || position.IsProtected) return;

            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    $"Вы уверены, что хотите удалить должность \"{position.Name}\"?\n\n" +
                    "Это действие нельзя отменить!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var deleteResult = await _positionService.DeletePositionAsync(position.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (deleteResult.Success)
                    {
                        AllPositions.Remove(position);
                        MessageBox.Show("Должность успешно удалена!", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(deleteResult.Message, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка удаления должности: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private async Task ShowErrorAsync(string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose)
                return;

            base.DisposeManagedResources();

            Application.Current.Dispatcher.Invoke(() =>
            {
                AllUsers?.Clear();
                FilteredUsers?.Clear();
                StatusFilters?.Clear();
                AllPositions?.Clear();
            });

            SearchText = null;
            UserToDelete = null;
            UserToManage = null;
            NewPositionName = null;
            EditPositionName = null;
            PositionToEdit = null;

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();

            if (_positionService is IDisposable positionDisposable)
                positionDisposable.Dispose();
        }
    }

    public class UserListStatusFilter : BaseViewModel
    {
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }
}