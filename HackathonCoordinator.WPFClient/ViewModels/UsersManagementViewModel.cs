using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class UsersManagementViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private readonly NavigationService _navigationService;

        private ObservableCollection<UserDto> _allUsers = new();
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

        private ObservableCollection<UserDto> _filteredUsers = new();
        public ObservableCollection<UserDto> FilteredUsers
        {
            get => _filteredUsers;
            set => SetProperty(ref _filteredUsers, value);
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterUsers();
            }
        }

        private UserDto _userToDelete;
        public UserDto UserToDelete
        {
            get => _userToDelete;
            set => SetProperty(ref _userToDelete, value);
        }

        private bool _showDeleteDialog;
        public bool ShowDeleteDialog
        {
            get => _showDeleteDialog;
            set => SetProperty(ref _showDeleteDialog, value);
        }

        public int TotalUsersCount => AllUsers?.Count ?? 0;
        public int UsersInTeamsCount => AllUsers?.Count(u => u.TeamId.HasValue) ?? 0;
        public int UsersWithoutTeamCount => AllUsers?.Count(u => !u.TeamId.HasValue) ?? 0;
        public bool HasNoUsers => !(FilteredUsers?.Any() ?? false);

        public ObservableCollection<UserListStatusFilter> StatusFilters { get; } = new()
        {
            new UserListStatusFilter { Id = 0, Name = "Все участники" },
            new UserListStatusFilter { Id = 1, Name = "В командах" },
            new UserListStatusFilter { Id = 2, Name = "Без команды" },
            new UserListStatusFilter { Id = 3, Name = "С GitHub" },
            new UserListStatusFilter { Id = 4, Name = "Капитаны" }
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

        // AsyncRelayCommand для операций с API
        public ICommand BackCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ViewUserCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public UsersManagementViewModel()
        {
            _userService = new UserService();
            _navigationService = App.NavigationService;

            BackCommand = new RelayCommand(GoBack);

            // AsyncRelayCommand для удаления пользователя
            DeleteUserCommand = new AsyncRelayCommand<UserDto>(
                execute: async (user) => await DeleteUserAsync(user),
                canExecute: (user) => user != null);

            ViewUserCommand = new RelayCommand<UserDto>(user => ViewUserDetails(user));
            ResetFiltersCommand = new RelayCommand(ResetFilters);

            // AsyncRelayCommand для подтверждения удаления
            ConfirmDeleteCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteDeleteUserAsync(),
                canExecute: () => UserToDelete != null);

            CancelDeleteCommand = new RelayCommand(() => CancelDelete());

            // AsyncRelayCommand для обновления
            RefreshCommand = new AsyncRelayCommand(
                execute: async () => await LoadUsersAsync(),
                canExecute: () => true);

            SelectedStatusFilter = StatusFilters[0];
            LoadUsersAsync();
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

        private async Task LoadUsersAsync()
        {
            try
            {
                var result = await _userService.GetAllUsersAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (result.Success)
                    {
                        AllUsers = new ObservableCollection<UserDto>(result.Data);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка загрузки пользователей: {result.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}\n\nПроверьте подключение к серверу.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task DeleteUserAsync(UserDto user)
        {
            if (user == null) return;

            UserToDelete = user;
            ShowDeleteDialog = true;
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
                        4 => filtered.Where(u => u.RoleId == 1),
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
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void ViewUserDetails(UserDto user)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (user != null)
                {
                    MessageBox.Show($"Детали пользователя:\n\n" +
                                  $"Имя: {user.Username}\n" +
                                  $"Email: {user.Email}\n" +
                                  $"Роль: {user.RoleName}\n" +
                                  $"GitHub: {user.GitHubUsername ?? "Не привязан"}\n" +
                                  $"Команда: {user.TeamName ?? "Не в команде"}",
                                  "Информация о пользователе");
                }
            });
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            Application.Current.Dispatcher.Invoke(() =>
            {
                AllUsers?.Clear();
                FilteredUsers?.Clear();
                StatusFilters?.Clear();
            });

            SearchText = null;
            UserToDelete = null;

            if (_userService is IDisposable disposable)
                disposable.Dispose();
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