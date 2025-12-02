using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.Storages;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class NotificationsViewModel : BaseViewModel
    {
        private readonly NotificationService _notificationService;
        private readonly NavigationService _navigationService;
        private readonly UserService _userService;
        private readonly ChatService _chatService;
        private readonly CompetitionService _competitionService;

        private HubConnection _hubConnection;
        private bool _isConnected;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set => SetProperty(ref _unreadCount, value);
        }

        public ObservableCollection<NotificationDto> Notifications { get; } = new();
        public ObservableCollection<NotificationDto> UnreadNotifications { get; } = new();
        public ObservableCollection<NotificationDto> ReadNotifications { get; } = new();

        public bool HasNotifications => Notifications.Any();
        public bool HasUnreadNotifications => UnreadNotifications.Any();
        public bool HasReadNotifications => ReadNotifications.Any();
        public bool HasNoNotifications => !HasNotifications;

        // AsyncRelayCommand для всех операций
        public ICommand LoadNotificationsCommand { get; }
        public ICommand MarkAsReadCommand { get; }
        public ICommand MarkAllAsReadCommand { get; }
        public ICommand DeleteNotificationCommand { get; }
        public ICommand OpenRelatedEntityCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }

        public NotificationsViewModel()
        {
            _notificationService = new NotificationService();
            _navigationService = App.NavigationService;
            _userService = new UserService();
            _chatService = new ChatService();
            _competitionService = new CompetitionService();

            // AsyncRelayCommand для загрузки уведомлений
            LoadNotificationsCommand = new AsyncRelayCommand(
                execute: async () => await LoadNotificationsAsync(),
                canExecute: () => true);

            // AsyncRelayCommand для отметки прочитанным
            MarkAsReadCommand = new AsyncRelayCommand<NotificationDto>(
                execute: async (notification) => await MarkAsReadAsync(notification),
                canExecute: (notification) => notification != null && !notification.IsRead);

            // AsyncRelayCommand для отметки всех прочитанными
            MarkAllAsReadCommand = new AsyncRelayCommand(
                execute: async () => await MarkAllAsReadAsync(),
                canExecute: () => UnreadNotifications.Any());

            // AsyncRelayCommand для удаления
            DeleteNotificationCommand = new AsyncRelayCommand<NotificationDto>(
                execute: async (notification) => await DeleteNotificationAsync(notification),
                canExecute: (notification) => notification != null);

            // AsyncRelayCommand для открытия связанной сущности
            OpenRelatedEntityCommand = new AsyncRelayCommand<NotificationDto>(
                execute: async (notification) => await OpenRelatedEntityAsync(notification),
                canExecute: (notification) => notification != null);

            BackCommand = new RelayCommand(GoBack);

            // AsyncRelayCommand для обновления
            RefreshCommand = new AsyncRelayCommand(
                execute: async () => await LoadNotificationsAsync(),
                canExecute: () => true);

            InitializeSignalR();
            LoadNotificationsCommand.Execute(null);
            LoadUnreadCountAsync();
        }

        private async void InitializeSignalR()
        {
            var baseUrl = "http://localhost:5046";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/notificationhub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(SecureTokenStorage.GetToken());
                })
                .WithAutomaticReconnect()
                .Build();

            SetupSignalREvents();

            try
            {
                await _hubConnection.StartAsync();
                _isConnected = true;
                await _hubConnection.InvokeAsync("SubscribeToUserNotifications");
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка подключения к уведомлениям: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void SetupSignalREvents()
        {
            _hubConnection.On<NotificationDto>("ReceiveNotification", OnNotificationReceived);
            _hubConnection.On<int>("UpdateUnreadCount", OnUnreadCountUpdated);
        }

        private void OnNotificationReceived(NotificationDto notification)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Notifications.Insert(0, notification);
                UnreadNotifications.Insert(0, notification);
                UpdateProperties();
            });
        }

        private void OnUnreadCountUpdated(int unreadCount)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UnreadCount = unreadCount;
            });
        }

        public async Task LoadNotificationsAsync()
        {
            IsLoading = true;

            try
            {
                var response = await _notificationService.GetUserNotificationsAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Notifications.Clear();
                    UnreadNotifications.Clear();
                    ReadNotifications.Clear();

                    if (response.Success)
                    {
                        foreach (var notification in response.Data)
                        {
                            Notifications.Add(notification);

                            if (notification.IsRead)
                                ReadNotifications.Add(notification);
                            else
                                UnreadNotifications.Add(notification);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка загрузки уведомлений: {response.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    UpdateProperties();
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}\n\nПроверьте подключение к серверу.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void LoadUnreadCountAsync()
        {
            try
            {
                var response = await _notificationService.GetUnreadCountAsync();
                if (response.Success)
                {
                    UnreadCount = response.Data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки счетчика уведомлений: {ex.Message}");
            }
        }

        private async Task MarkAsReadAsync(NotificationDto notification)
        {
            try
            {
                var response = await _notificationService.MarkAsReadAsync(notification.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (response.Success)
                    {
                        notification.IsRead = true;
                        UnreadNotifications.Remove(notification);
                        ReadNotifications.Insert(0, notification);
                        UpdateProperties();
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка: {response.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка отметки уведомления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task MarkAllAsReadAsync()
        {
            try
            {
                var response = await _notificationService.MarkAllAsReadAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (response.Success)
                    {
                        foreach (var notification in UnreadNotifications.ToList())
                        {
                            notification.IsRead = true;
                            ReadNotifications.Add(notification);
                        }
                        UnreadNotifications.Clear();
                        UpdateProperties();
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка: {response.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка отметки всех уведомлений: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task DeleteNotificationAsync(NotificationDto notification)
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите удалить это уведомление?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var response = await _notificationService.DeleteNotificationAsync(notification.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (response.Success)
                    {
                        Notifications.Remove(notification);
                        if (notification.IsRead)
                            ReadNotifications.Remove(notification);
                        else
                            UnreadNotifications.Remove(notification);

                        UpdateProperties();
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка: {response.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка удаления уведомления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task OpenRelatedEntityAsync(NotificationDto notification)
        {
            try
            {
                if (!notification.IsRead)
                {
                    await MarkAsReadAsync(notification);
                }

                if (notification.RelatedEntityType == "task" && notification.RelatedEntityId.HasValue)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _navigationService.NavigateTo(new TaskDetailsPage(notification.RelatedEntityId.Value));
                    });
                }
                else if (notification.RelatedEntityType == "team" && notification.RelatedEntityId.HasValue)
                {
                    var user = await _userService.GetCurrentUserAsync();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (user.Data.RoleId == 3)
                            _navigationService.NavigateTo(new TeamPage(notification.RelatedEntityId.Value));
                        else
                            _navigationService.NavigateTo(new TeamPage());
                    });
                }
                else if (notification.RelatedEntityType == "competition" && notification.RelatedEntityId.HasValue)
                {
                    var competition = await _competitionService.GetCompetitionAsync(notification.RelatedEntityId.Value);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _navigationService.NavigateTo(new CompetitionDetailsPage(competition.Data));
                    });
                }
                else if (notification.RelatedEntityType == "team chat" && notification.RelatedEntityId.HasValue)
                {
                    var chatPage = new ChatPage();
                    var viewModel = chatPage.DataContext as ChatViewModel;

                    if (viewModel != null)
                    {
                        await viewModel.LoadTeamChatAsync(notification.RelatedEntityId.Value);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _navigationService.NavigateTo(chatPage);
                        });
                    }
                }
                else if (notification.RelatedEntityType == "task chat" && notification.RelatedEntityId.HasValue)
                {
                    var chatPage = new ChatPage();
                    var viewModel = chatPage.DataContext as ChatViewModel;

                    if (viewModel != null)
                    {
                        await viewModel.LoadTaskChatAsync(notification.RelatedEntityId.Value);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _navigationService.NavigateTo(chatPage);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка открытия связанной сущности: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
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

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(HasNotifications));
            OnPropertyChanged(nameof(HasUnreadNotifications));
            OnPropertyChanged(nameof(HasReadNotifications));
            OnPropertyChanged(nameof(HasNoNotifications));
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Notifications?.Clear();
                UnreadNotifications?.Clear();
                ReadNotifications?.Clear();
            });

            if (_hubConnection != null)
            {
                _hubConnection.DisposeAsync();
            }

            if (_notificationService is IDisposable notificationDisposable)
                notificationDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();

            if (_chatService is IDisposable chatDisposable)
                chatDisposable.Dispose();

            if (_competitionService is IDisposable competitionDisposable)
                competitionDisposable.Dispose();
        }
    }
}