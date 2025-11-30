// ViewModels/NotificationsViewModel.cs
using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.Storages;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class NotificationsViewModel : INotifyPropertyChanged
    {
        private readonly NotificationService _notificationService;
        private readonly NavigationService _navigationService;
        private readonly UserService _userService;
        private readonly CompetitionService _competitionService;
        private HubConnection _hubConnection;
        private bool _isConnected;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set { _unreadCount = value; OnPropertyChanged(); }
        }

        public ObservableCollection<NotificationDto> Notifications { get; } = new();
        public ObservableCollection<NotificationDto> UnreadNotifications { get; } = new();
        public ObservableCollection<NotificationDto> ReadNotifications { get; } = new();

        public bool HasNotifications => Notifications.Any();
        public bool HasUnreadNotifications => UnreadNotifications.Any();
        public bool HasReadNotifications => ReadNotifications.Any();
        public bool HasNoNotifications => !HasNotifications;

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
            _competitionService = new CompetitionService();

            LoadNotificationsCommand = new RelayCommand(async () => await LoadNotificationsAsync());
            MarkAsReadCommand = new RelayCommand<NotificationDto>(async (notification) => await MarkAsReadAsync(notification));
            MarkAllAsReadCommand = new RelayCommand(async () => await MarkAllAsReadAsync());
            DeleteNotificationCommand = new RelayCommand<NotificationDto>(async (notification) => await DeleteNotificationAsync(notification));
            OpenRelatedEntityCommand = new RelayCommand<NotificationDto>(OpenRelatedEntity);
            BackCommand = new RelayCommand(GoBack);
            RefreshCommand = new RelayCommand(async () => await LoadNotificationsAsync());

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
                MessageBox.Show($"Ошибка подключения к уведомлениям: {ex.Message}");
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
                if (response.Success)
                {
                    Notifications.Clear();
                    UnreadNotifications.Clear();
                    ReadNotifications.Clear();

                    foreach (var notification in response.Data)
                    {
                        Notifications.Add(notification);

                        if (notification.IsRead)
                            ReadNotifications.Add(notification);
                        else
                            UnreadNotifications.Add(notification);
                    }

                    UpdateProperties();
                }
                else
                {
                    MessageBox.Show($"Ошибка загрузки уведомлений: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}");
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
                // Игнорируем ошибки при загрузке счетчика
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки счетчика уведомлений: {ex.Message}");
            }
        }

        private async Task MarkAsReadAsync(NotificationDto notification)
        {
            if (notification.IsRead) return;

            try
            {
                var response = await _notificationService.MarkAsReadAsync(notification.Id);
                if (response.Success)
                {
                    notification.IsRead = true;
                    UnreadNotifications.Remove(notification);
                    ReadNotifications.Add(notification);
                    UpdateProperties();
                }
                else
                {
                    MessageBox.Show($"Ошибка: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отметки уведомления: {ex.Message}");
            }
        }

        private async Task MarkAllAsReadAsync()
        {
            try
            {
                var response = await _notificationService.MarkAllAsReadAsync();
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
                    MessageBox.Show($"Ошибка: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отметки всех уведомлений: {ex.Message}");
            }
        }

        private async Task DeleteNotificationAsync(NotificationDto notification)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить это уведомление?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var response = await _notificationService.DeleteNotificationAsync(notification.Id);
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
                    MessageBox.Show($"Ошибка: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления уведомления: {ex.Message}");
            }
        }

        private async void OpenRelatedEntity(NotificationDto notification)
        {
            if (notification.RelatedEntityType == "task" && notification.RelatedEntityId.HasValue)
            {
                _navigationService.NavigateTo(new TaskDetailsPage(notification.RelatedEntityId.Value));
            }
            else if (notification.RelatedEntityType == "team" && notification.RelatedEntityId.HasValue)
            {
                var user = await _userService.GetCurrentUserAsync();

                if (user.Data.RoleId == 3)
                    _navigationService.NavigateTo(new TeamPage(notification.RelatedEntityId.Value));
                else
                    _navigationService.NavigateTo(new TeamPage());
            }
            else if (notification.RelatedEntityType == "competition" && notification.RelatedEntityId.HasValue)
            {
                var competition = await _competitionService.GetCompetitionAsync(notification.RelatedEntityId.Value);
                _navigationService.NavigateTo(new CompetitionDetailsPage(competition.Data));
            }
            // Автоматически отмечаем как прочитанное при открытии
            if (!notification.IsRead)
            {
                _ = MarkAsReadAsync(notification);
            }
        }

        private void GoBack()
        {
            _navigationService.GoBack();
        }

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(HasNotifications));
            OnPropertyChanged(nameof(HasUnreadNotifications));
            OnPropertyChanged(nameof(HasReadNotifications));
            OnPropertyChanged(nameof(HasNoNotifications));
        }

        public async void Dispose()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}