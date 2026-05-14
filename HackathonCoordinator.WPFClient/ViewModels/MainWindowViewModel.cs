using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.Storages;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.AspNetCore.SignalR.Client;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        private readonly TeamService _teamService;
        private readonly UserService _userService;
        private readonly AuthService _authService;

        private HubConnection _notificationHubConnection;
        private readonly string[] _themes = { "Light", "Dark", "Summer", "Spring", "Winter", "Autumn" };
        private int _currentThemeIndex = 0;
        private Type _currentPageType;
        private bool _isOrganizer = false;
        public bool IsOrganizer
        {
            get => _isOrganizer;
            set => SetProperty(ref _isOrganizer, value);
        }

        private bool _isAdmin = false;
        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        private bool _notificationHubConnected = false;
        private int _unreadNotificationsCount;

        public int UnreadNotificationsCount
        {
            get => _unreadNotificationsCount;
            set
            {
                SetProperty(ref _unreadNotificationsCount, value);
                OnPropertyChanged(nameof(HasUnreadNotifications));
                OnPropertyChanged(nameof(NotificationsButtonText));
            }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        // Вычисляемые свойства
        public bool HasUnreadNotifications => UnreadNotificationsCount > 0;
        public string NotificationsButtonText => HasUnreadNotifications
            ? $"🔔 ({UnreadNotificationsCount})"
            : "🔔";
        public string CurrentThemeName => _themes[_currentThemeIndex];

        // Команды - ВСЕ асинхронные команды используем AsyncRelayCommand
        public ICommand OpenProfileCommand { get; }
        public ICommand OpenMainPageCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenUsersManagementCommand { get; }
        public ICommand OpenChatsCommand { get; }
        public ICommand OpenNotificationsCommand { get; }

        public MainWindowViewModel()
        {
            _teamService = new TeamService();
            _userService = new UserService();
            _authService = new AuthService();

            // Инициализация команд
            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            // Асинхронные команды используем AsyncRelayCommand
            OpenNotificationsCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteOpenNotificationsAsync(),
                canExecute: () => _navigationService != null);

            OpenMainPageCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteOpenMainPageAsync(),
                canExecute: () => _navigationService != null);

            OpenChatsCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteOpenChatsAsync(),
                canExecute: () => _navigationService != null);

            OpenProfileCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteOpenProfileAsync(),
                canExecute: () => _navigationService != null);

            LogoutCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteLogoutAsync(),
                canExecute: () => true);

            OpenUsersManagementCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteOpenUsersManagementAsync(),
                canExecute: () => IsOrganizer && _navigationService != null);

            InitializeNotificationsSignalR();

            // Очистка ресурсов при выходе
            Application.Current.Exit += async (s, e) => await DisposeNotificationHub();
        }

        public void SetCurrentPageType(Type pageType)
        {
            _currentPageType = pageType;
        }

        private async Task ExecuteOpenMainPageAsync()
        {
            try
            {
                await OpenMainPage();
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки главной страницы: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _navigationService.NavigateTo(new CompetitionsPage());
                });
            }
        }

        private async Task ExecuteOpenChatsAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _navigationService.NavigateToWithClearHistory(new ChatsPage());
            });
        }

        private async Task ExecuteOpenProfileAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _navigationService.NavigateToWithClearHistory(new ProfilePage());
            });
        }

        private async Task ExecuteOpenUsersManagementAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _navigationService.NavigateToWithClearHistory(new UsersManagementPage());
            });
        }

        private async Task ExecuteOpenNotificationsAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _navigationService.NavigateToWithClearHistory(new NotificationsPage());
            });
        }

        private async Task ExecuteLogoutAsync()
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result == MessageBoxResult.Yes)
            {
                await Logout();
            }
        }

        private async Task Logout()
        {
            try
            {
                await DisposeNotificationHub();
                _authService.Logout();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _navigationService.NavigateTo(new AuthorizationPage());
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка при выходе: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                    Application.Current.Shutdown();
                });
            }
        }

        private async void LoadUnreadNotificationsCount()
        {
            try
            {
                var notificationService = new NotificationService();
                var response = await notificationService.GetUnreadCountAsync();

                if (response.Success)
                {
                    UnreadNotificationsCount = response.Data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки счетчика уведомлений: {ex.Message}");
            }
        }

        public async void InitializeNotificationsSignalR()
        {
            var baseUrl = "http://localhost:5046";

            _notificationHubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/notificationhub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(SecureTokenStorage.GetToken());
                })
                .WithAutomaticReconnect(new[] {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                })
                .Build();

            SetupNotificationHubEvents();

            try
            {
                await _notificationHubConnection.StartAsync();
                _notificationHubConnected = true;

                await _notificationHubConnection.InvokeAsync("SubscribeToUserNotifications");
                LoadUnreadNotificationsCount();

                System.Diagnostics.Debug.WriteLine("Успешно подключен к хабу уведомлений");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подключения к уведомлениям: {ex.Message}");
            }
        }

        private void SetupNotificationHubEvents()
        {
            _notificationHubConnection.Closed += async (error) =>
            {
                _notificationHubConnected = false;
                System.Diagnostics.Debug.WriteLine("Соединение с уведомлениями разорвано");

                await Task.Delay(5000);
                InitializeNotificationsSignalR();
            };

            _notificationHubConnection.Reconnected += (connectionId) =>
            {
                _notificationHubConnected = true;
                System.Diagnostics.Debug.WriteLine("Переподключен к уведомлениям");
                return Task.CompletedTask;
            };

            // Получение обновления счетчика непрочитанных
            _notificationHubConnection.On<int>("UpdateUnreadCount", (unreadCount) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UnreadNotificationsCount = unreadCount;
                });
            });

            // Получение нового уведомления
            _notificationHubConnection.On<NotificationDto>("ReceiveNotification", async (notification) =>
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    UnreadNotificationsCount++;

                    if (notification.NotificationTypeId == 23)
                    {
                        var result = MessageBox.Show(
                            $"{notification.Message}\n\n" +
                            "Будет совершен выход из аккаунта!",
                            notification.Title,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.OK)
                        {
                            await Logout();
                        }
                    }
                    else if (notification.NotificationTypeId == (int)NotificationTypes.TeamDeleted)
                    {
                        if (_currentPageType == typeof(TeamPage) || _currentPageType == typeof(ChatPage))
                        {
                            var result = MessageBox.Show(
                                $"{notification.Message}\n\n" +
                                "Вы будете перенаправлены на главную страницу.",
                                notification.Title,
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            if (result == MessageBoxResult.OK)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(async () =>
                                {
                                    await OpenMainPage();
                                });
                            }
                        }
                    }
                    else if (notification.NotificationTypeId == (int)NotificationTypes.CompetitionDeleted)
                    {
                        if (_currentPageType == typeof(CompetitionDetailsPage))
                        {
                            var result = MessageBox.Show(
                                $"{notification.Message}\n\n" +
                                "Вы будете перенаправлены на главную страницу.",
                                notification.Title,
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            if (result == MessageBoxResult.OK)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(async () =>
                                {
                                    await OpenMainPage();
                                });
                            }
                        }
                    }
                });
            });
        }

        public async Task DisposeNotificationHub()
        {
            if (_notificationHubConnection != null)
            {
                try
                {
                    await _notificationHubConnection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии соединения: {ex.Message}");
                }
            }
        }

        public async void CheckUserRole()
        {
            var user = await _userService.GetCurrentUserAsync();
            IsAdmin = user.Data.RoleId == (int)Roles.Admin;
            IsOrganizer = user.Data.RoleId == (int)Roles.Organizer || IsAdmin;
            OnPropertyChanged(nameof(IsOrganizer));
        }

        private void ToggleTheme()
        {
            _currentThemeIndex = (_currentThemeIndex + 1) % _themes.Length;
            App.SwitchTheme(_themes[_currentThemeIndex]);
            OnPropertyChanged(nameof(CurrentThemeName));
        }

        public async void GetUsername()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                Username = user.Data.Username ?? "Гость";
            }
            catch
            {
                Username = "Гость";
            }
        }

        public async Task OpenMainPage()
        {
            var teamResponse = await _teamService.GetCurrentTeamAsync();

            if (!teamResponse.Success || teamResponse.Data == null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _navigationService.NavigateToWithClearHistory(new CompetitionsPage());
                });
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _navigationService.NavigateToWithClearHistory(new TeamPage(teamResponse.Data));
                });
            }
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            if (_teamService is IDisposable teamDisposable)
                teamDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();

            if (_authService is IDisposable authDisposable)
                authDisposable.Dispose();

            _notificationHubConnection?.DisposeAsync();
        }
    }
}