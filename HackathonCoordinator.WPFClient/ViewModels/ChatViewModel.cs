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
    public class ChatViewModel : BaseViewModel
    {
        private readonly ChatService _chatService;
        private readonly UserService _userService;
        private readonly NavigationService _navigationService;

        private HubConnection _hubConnection;
        private ChatDto _currentChat;
        private UserDto _currentUser;

        private bool _isConnected;
        private bool _isCaptain;
        private bool _isLoading;
        private string _chatTitle = "";
        private string _newMessageText = "";

        public ChatDto CurrentChat
        {
            get => _currentChat;
            set
            {
                if (_currentChat != null && _isConnected)
                {
                    _ = LeaveCurrentChatAsync();
                }

                SetProperty(ref _currentChat, value);
            }
        }

        public UserDto CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public string NewMessageText
        {
            get => _newMessageText;
            set
            {
                SetProperty(ref _newMessageText, value);
                OnPropertyChanged(nameof(CanSendMessage));
            }
        }

        public bool IsCaptain
        {
            get => _isCaptain;
            set => SetProperty(ref _isCaptain, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ChatTitle
        {
            get => _chatTitle;
            set => SetProperty(ref _chatTitle, value);
        }

        public bool CanSendMessage => !string.IsNullOrWhiteSpace(NewMessageText) && !IsLoading && _isConnected;
        public bool HasNoMessages => !Messages.Any();

        public ObservableCollection<MessageDto> Messages { get; } = new();
        public ObservableCollection<ChatParticipantDto> Participants { get; } = new();

        // AsyncRelayCommand для всех операций
        public ICommand SendMessageCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand EditMessageCommand { get; }
        public ICommand DeleteMessageCommand { get; }

        public ChatViewModel()
        {
            _chatService = new ChatService();
            _userService = new UserService();
            _navigationService = App.NavigationService;

            // AsyncRelayCommand для отправки сообщений
            SendMessageCommand = new AsyncRelayCommand(
                execute: async () => await SendMessageAsync(),
                canExecute: () => CanSendMessage);

            BackCommand = new RelayCommand(GoBack);

            // AsyncRelayCommand для редактирования сообщений
            EditMessageCommand = new AsyncRelayCommand<MessageDto>(
                execute: async (msg) => await EditMessageAsync(msg),
                canExecute: (msg) => msg != null && msg.IsMyMessage);

            // AsyncRelayCommand для удаления сообщений
            DeleteMessageCommand = new AsyncRelayCommand<MessageDto>(
                execute: async (msg) => await DeleteMessageAsync(msg),
                canExecute: (msg) => msg != null && msg.IsMyMessage);

            InitializeSignalRAsync();
            LoadCurrentUser();
        }

        private async void LoadCurrentUser()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user.Success)
                {
                    CurrentUser = user.Data;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки пользователя: {ex.Message}");
            }
        }

        private async Task InitializeSignalRAsync()
        {
            var baseUrl = "https://zip.hhallva.ru";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/chathub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(SecureTokenStorage.GetToken());
                })
                .WithAutomaticReconnect(GetReconnectDelays())
                .Build();

            SetupConnectionEvents();
            SetupSignalREvents();

            _ = ConnectToHubAsync();
        }

        private TimeSpan[] GetReconnectDelays() => new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)
        };

        private void SetupConnectionEvents()
        {
            _hubConnection.Closed += OnConnectionClosed;
            _hubConnection.Reconnected += OnConnectionReconnected;
        }

        private async Task OnConnectionClosed(Exception error)
        {
            _isConnected = false;
            OnPropertyChanged(nameof(CanSendMessage));

            await Task.Delay(5000);
            await ConnectToHubAsync();
        }

        private Task OnConnectionReconnected(string connectionId)
        {
            _isConnected = true;
            OnPropertyChanged(nameof(CanSendMessage));

            if (CurrentChat != null)
            {
                _ = JoinCurrentChatAsync();
            }

            return Task.CompletedTask;
        }

        private async Task ConnectToHubAsync()
        {
            try
            {
                await _hubConnection.StartAsync();
                _isConnected = true;
                OnPropertyChanged(nameof(CanSendMessage));
                await _hubConnection.InvokeAsync("JoinChat", CurrentChat.Id);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка подключения к чату: {ex.Message}");
            }
        }

        private void SetupSignalREvents()
        {
            _hubConnection.On<MessageDto>("ReceiveMessage", OnMessageReceived);
            _hubConnection.On<int, string>("MessageEdited", OnMessageEdited);
            _hubConnection.On<int>("MessageDeleted", OnMessageDeleted);
        }

        private void OnMessageReceived(MessageDto message)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser.Success)
                {
                    message.IsMyMessage = message.UserId == currentUser.Data.Id;
                }
                Messages.Add(message);
                ScrollToBottom();
            });
        }

        private void OnMessageEdited(int messageId, string newText)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var message = Messages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    message.Text = newText;
                    message.IsEdited = true;
                }
            });
        }

        private void OnMessageDeleted(int messageId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var message = Messages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    Messages.Remove(message);
                }
            });
        }

        public async Task LoadTeamChatAsync(ChatDto chat)
        {
            IsLoading = true;

            try
            {
                CurrentChat = chat;
                ChatTitle = $"💬 Чат команды";
                await UpdateMessagesAndParticipantsAsync();

                CheckIfCurrentUserIsCaptain();

                if (_isConnected)
                    await _hubConnection.InvokeAsync("JoinChat", CurrentChat.Id);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки чата: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadTaskChatAsync(ChatDto chat)
        {
            IsLoading = true;

            try
            {
                CurrentChat = chat;
                ChatTitle = $"💬 Чат задачи";
                await UpdateMessagesAndParticipantsAsync();

                CheckIfCurrentUserIsCaptain();

                if (_isConnected)
                    await _hubConnection.InvokeAsync("JoinChat", CurrentChat.Id);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки чата: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CheckIfCurrentUserIsCaptain()
        {
            if (CurrentUser != null)
            {
                IsCaptain = CurrentUser.RoleId == 1;
            }
            else
            {
                IsCaptain = false;
            }
        }

        private async Task JoinCurrentChatAsync()
        {
            if (_isConnected && CurrentChat != null)
            {
                try
                {
                    await _hubConnection.InvokeAsync("JoinChat", CurrentChat.Id);
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync($"Ошибка присоединения к чату: {ex.Message}");
                }
            }
        }

        private async Task LeaveCurrentChatAsync()
        {
            if (_isConnected && CurrentChat != null)
            {
                try
                {
                    await _hubConnection.InvokeAsync("LeaveChat", CurrentChat.Id);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка выхода из чата: {ex.Message}");
                }
            }
        }

        private async Task UpdateMessagesAndParticipantsAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Messages.Clear();
                foreach (var message in CurrentChat.Messages)
                {
                    Messages.Add(message);
                }

                Participants.Clear();
                foreach (var participant in CurrentChat.Participants)
                {
                    Participants.Add(participant);
                }

                OnPropertyChanged(nameof(HasNoMessages));
                ScrollToBottom();
            });
        }

        private async Task SendMessageAsync()
        {
            try
            {
                await _hubConnection.InvokeAsync("TestConnection", "Тестовое сообщение");
                System.Diagnostics.Debug.WriteLine("✅ TestConnection вызван");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TestConnection ошибка: {ex.Message}");
            }

            if (!CanSendMessage) return;

            var messageText = NewMessageText.Trim();
            NewMessageText = "";

            try
            {
                var result = await _chatService.SendMessageAsync(CurrentChat.Id, messageText);
                if (!result.Success)
                {
                    await ShowErrorAsync(result.Message);
                    NewMessageText = messageText;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка отправки сообщения: {ex.Message}");
                NewMessageText = messageText;
            }
        }

        private async Task EditMessageAsync(MessageDto message)
        {
            var newText = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return Microsoft.VisualBasic.Interaction.InputBox(
                    "Редактировать сообщение:",
                    "Редактирование",
                    message.Text);
            });

            if (!string.IsNullOrWhiteSpace(newText) && newText != message.Text)
            {
                try
                {
                    var result = await _chatService.EditMessageAsync(message.Id, newText);
                    if (!result.Success)
                    {
                        await ShowErrorAsync(result.Message);
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync($"Ошибка редактирования сообщения: {ex.Message}");
                }
            }
        }

        private async Task DeleteMessageAsync(MessageDto message)
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите удалить это сообщение?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var deleteResult = await _chatService.DeleteMessageAsync(message.Id);
                    if (!deleteResult.Success)
                    {
                        await ShowErrorAsync(deleteResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync($"Ошибка удаления сообщения: {ex.Message}");
                }
            }
        }

        private void GoBack()
        {
            _ = LeaveCurrentChatAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CurrentChat?.TeamId != null)
                {
                    var targetPage = CurrentUser?.RoleId == 3
                        ? new TeamPage(CurrentChat.TeamId.Value)
                        : new TeamPage();
                    _navigationService.NavigateTo(targetPage);
                }
                else if (CurrentChat?.TaskId != null)
                {
                    _navigationService.NavigateTo(new TaskDetailsPage(CurrentChat.TaskId.Value));
                }
                else
                {
                    _navigationService.NavigateTo(new CompetitionsPage());
                }
            });
        }

        private void ScrollToBottom()
        {
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
        }

        private async Task ShowErrorAsync(string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        public event EventHandler ScrollToBottomRequested;

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages?.Clear();
                Participants?.Clear();
            });

            if (_hubConnection != null)
            {
                _hubConnection.Closed -= OnConnectionClosed;
                _hubConnection.Reconnected -= OnConnectionReconnected;
            }

            _ = LeaveCurrentChatAsync();
            _hubConnection?.DisposeAsync().GetAwaiter().GetResult();

            if (_chatService is IDisposable chatDisposable)
                chatDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();
        }
    }
}