using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
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
        public bool doDispose = true;
        private bool _isInitialized = false;

        private readonly NotificationService _notificationService;
        private readonly UserService _userService;
        private readonly TeamService _teamService;
        private readonly TaskService _taskService;
        private readonly ChatService _chatService;
        private readonly CompetitionService _competitionService;

        private HubConnection _hubConnection;
        private bool _isConnected;

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

        // Команды
        public ICommand MarkAsReadCommand { get; }
        public ICommand MarkAllAsReadCommand { get; }
        public ICommand DeleteNotificationCommand { get; }
        public ICommand OpenRelatedEntityCommand { get; }
        public ICommand RefreshCommand { get; }

        public NotificationsViewModel()
        {
            _notificationService = new NotificationService();
            _userService = new UserService();
            _teamService = new TeamService();
            _taskService = new TaskService();
            _chatService = new ChatService();
            _competitionService = new CompetitionService();

            MarkAsReadCommand = new AsyncRelayCommand<NotificationDto>(
                execute: async (notification) => await MarkAsReadAsync(notification),
                canExecute: (notification) => notification != null && !notification.IsRead);

            MarkAllAsReadCommand = new AsyncRelayCommand(
                execute: async () => await MarkAllAsReadAsync(),
                canExecute: () => UnreadNotifications.Any());

            DeleteNotificationCommand = new AsyncRelayCommand<NotificationDto>(
                execute: async (notification) => await DeleteNotificationAsync(notification),
                canExecute: (notification) => notification != null);

            OpenRelatedEntityCommand = new AsyncRelayCommand<NotificationDto>(
                execute: async (notification) => await OpenRelatedEntityAsync(notification),
                canExecute: (notification) => notification != null);

            RefreshCommand = new AsyncRelayCommand(
                execute: async () => await RefreshAsync(),
                canExecute: () => true);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            IsLoading = true;

            try
            {
                await InitializeSignalR();
                await LoadNotificationsAsync();
                await LoadUnreadCountAsync();
                _isInitialized = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshAsync()
        {
            await LoadNotificationsAsync();
            await LoadUnreadCountAsync();
        }

        private async Task InitializeSignalR()
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
                await ShowErrorAsync($"Ошибка подключения к уведомлениям: {ex.Message}");
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

        private async Task LoadNotificationsAsync()
        {
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
                await ShowErrorAsync($"Ошибка загрузки уведомлений: {ex.Message}");
            }
        }

        private async Task LoadUnreadCountAsync()
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
                await ShowErrorAsync($"Ошибка отметки уведомления: {ex.Message}");
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
                await ShowErrorAsync($"Ошибка отметки всех уведомлений: {ex.Message}");
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
                await ShowErrorAsync($"Ошибка удаления уведомления: {ex.Message}");
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
                    var task = await _taskService.GetTaskDetailsAsync(notification.RelatedEntityId.Value);

                    if (task.Success && task.Data != null)
                    {
                        doDispose = false;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _navigationService.NavigateTo(new TaskDetailsPage(task.Data));
                        });
                    }
                    else
                    {
                        await ShowErrorAsync($"Задача не найдена или была удалена.\n{task.Message}");
                    }
                }
                else if (notification.RelatedEntityType == "team" && notification.RelatedEntityId.HasValue)
                {
                    var team = await _teamService.GetTeamByIdAsync(notification.RelatedEntityId.Value);

                    if (team.Success && team.Data != null)
                    {
                        doDispose = false;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _navigationService.NavigateTo(new TeamPage(team.Data));
                        });
                    }
                    else
                    {
                        await ShowErrorAsync($"Команда не найдена или была удалена.\n{team.Message}");
                    }
                }
                else if (notification.RelatedEntityType == "competition" && notification.RelatedEntityId.HasValue)
                {
                    var competition = await _competitionService.GetCompetitionAsync(notification.RelatedEntityId.Value);

                    if (competition.Success && competition.Data != null)
                    {
                        doDispose = false;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _navigationService.NavigateTo(new CompetitionDetailsPage(competition.Data));
                        });
                    }
                    else
                    {
                        await ShowErrorAsync($"Соревнование не найдено или было удалено.\n{competition.Message}");
                    }
                }
                else if (notification.RelatedEntityType == "team chat" && notification.RelatedEntityId.HasValue)
                {
                    var chat = await _chatService.GetTeamChatAsync(notification.RelatedEntityId.Value);

                    if (chat.Success && chat.Data != null)
                    {
                        doDispose = false;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var chatPage = new ChatPage(chat.Data, true);
                            _navigationService.NavigateTo(chatPage);
                        });
                    }
                    else
                    {
                        await ShowErrorAsync($"Чат команды не найден или был удален.\n{chat.Message}");
                    }
                }
                else if (notification.RelatedEntityType == "task chat" && notification.RelatedEntityId.HasValue)
                {
                    var chat = await _chatService.GetTaskChatAsync(notification.RelatedEntityId.Value);

                    if (chat.Success && chat.Data != null)
                    {
                        doDispose = false;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var chatPage = new ChatPage(chat.Data, false);
                            _navigationService.NavigateTo(chatPage);
                        });
                    }
                    else
                    {
                        await ShowErrorAsync($"Чат задачи не найден или был удален.\n{chat.Message}");
                    }
                }
                else
                {
                    await ShowErrorAsync("Не удалось определить тип связанной сущности");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка открытия связанной сущности: {ex.Message}");
            }
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
            if (!doDispose)
                return;

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