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
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly ChatService _chatService;
        private readonly UserService _userService;
        private readonly NavigationService _navigationService;
        private HubConnection _hubConnection;
        private bool _isConnected;
        private bool _disposed = false;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private ChatDto _currentChat;
        public ChatDto CurrentChat
        {
            get => _currentChat;
            set
            {
                // Покидаем предыдущий чат перед установкой нового
                if (_currentChat != null && _isConnected)
                {
                    _ = LeaveCurrentChatAsync();
                }

                _currentChat = value;
                OnPropertyChanged();
            }
        }

        private string _newMessageText = "";
        public string NewMessageText
        {
            get => _newMessageText;
            set
            {
                _newMessageText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSendMessage));
            }
        }

        private bool _isCaptain;
        public bool IsCaptain
        {
            get => _isCaptain;
            set { _isCaptain = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _chatTitle;
        public string ChatTitle
        {
            get => _chatTitle;
            set { _chatTitle = value; OnPropertyChanged(); }
        }

        private UserDto _currentUser;
        public UserDto CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        public bool CanSendMessage => !string.IsNullOrWhiteSpace(NewMessageText) && !IsLoading && _isConnected;
        public bool HasNoMessages => !Messages.Any();

        public ObservableCollection<MessageDto> Messages { get; } = new();
        public ObservableCollection<ChatParticipantDto> Participants { get; } = new();

        public ICommand SendMessageCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand EditMessageCommand { get; }
        public ICommand DeleteMessageCommand { get; }

        public ChatViewModel()
        {
            _chatService = new ChatService();
            _userService = new UserService();
            _navigationService = App.NavigationService;

            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(),
                () => CanSendMessage);
            BackCommand = new RelayCommand(GoBack);
            EditMessageCommand = new RelayCommand<MessageDto>(async (msg) => await EditMessageAsync(msg));
            DeleteMessageCommand = new RelayCommand<MessageDto>(async (msg) => await DeleteMessageAsync(msg));

            InitializeSignalR();
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
                ShowError($"Ошибка загрузки пользователя: {ex.Message}");
            }
        }

        private async void InitializeSignalR()
        {
            var baseUrl = "http://localhost:5046";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/chathub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(SecureTokenStorage.GetToken());
                })
                .WithAutomaticReconnect(GetReconnectDelays())
                .Build();

            SetupConnectionEvents();
            SetupSignalREvents();

            await ConnectToHubAsync();
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
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения к чату: {ex.Message}");
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

        public async Task LoadTeamChatAsync(int teamId)
        {
            IsLoading = true;

            try
            {
                var chat = await _chatService.GetTeamChatAsync(teamId);
                if (chat.Success)
                {
                    CurrentChat = chat.Data;
                    ChatTitle = $"💬 Чат команды";
                    UpdateMessagesAndParticipants();
                    OnPropertyChanged(nameof(HasNoMessages));

                    CheckIfCurrentUserIsCaptain();

                    if (_isConnected)
                        await _hubConnection.InvokeAsync("JoinChat", CurrentChat.Id);
                }
                else
                {
                    ShowError(chat.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки чата: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadTaskChatAsync(int taskId)
        {
            IsLoading = true;

            try
            {
                var chat = await _chatService.GetTaskChatAsync(taskId);
                if (chat.Success)
                {
                    CurrentChat = chat.Data;
                    ChatTitle = $"💬 Чат задачи";
                    UpdateMessagesAndParticipants();

                    CheckIfCurrentUserIsCaptain();

                    // Присоединяемся к чату через SignalR
                    if (_isConnected)
                        await _hubConnection.InvokeAsync("JoinChat", CurrentChat.Id);
                }
                else
                {
                    ShowError(chat.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки чата: {ex.Message}");
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
                    ShowError($"Ошибка присоединения к чату: {ex.Message}");
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
                    // Логируем, но не показываем пользователю - это не критическая ошибка
                    System.Diagnostics.Debug.WriteLine($"Ошибка выхода из чата: {ex.Message}");
                }
            }
        }

        private void UpdateMessagesAndParticipants()
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
        }

        private async Task SendMessageAsync()
        {
            if (!CanSendMessage) return;

            var messageText = NewMessageText.Trim();
            NewMessageText = "";

            try
            {
                var result = await _chatService.SendMessageAsync(CurrentChat.Id, messageText);
                if (!result.Success)
                {
                    ShowError(result.Message);
                    NewMessageText = messageText; // Возвращаем текст если ошибка
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка отправки сообщения: {ex.Message}");
                NewMessageText = messageText; // Возвращаем текст если ошибка
            }
        }

        private async Task EditMessageAsync(MessageDto message)
        {
            if (!message.IsMyMessage) return;

            var newText = Microsoft.VisualBasic.Interaction.InputBox(
                "Редактировать сообщение:",
                "Редактирование",
                message.Text);

            if (!string.IsNullOrWhiteSpace(newText) && newText != message.Text)
            {
                var result = await _chatService.EditMessageAsync(message.Id, newText);
                if (!result.Success)
                {
                    ShowError(result.Message);
                }
            }
        }

        private async Task DeleteMessageAsync(MessageDto message)
        {
            if (!message.IsMyMessage)
            {
                ShowError("Можно удалять только свои сообщения");
                return;
            }

            var result = MessageBox.Show(
                "Вы уверены, что хотите удалить это сообщение?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var deleteResult = await _chatService.DeleteMessageAsync(message.Id);
                if (!deleteResult.Success)
                {
                    ShowError(deleteResult.Message);
                }
            }
        }

        private void GoBack()
        {
            // Покидаем чат перед навигацией
            _ = LeaveCurrentChatAsync();

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
        }

        private void ScrollToBottom()
        {
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public event EventHandler ScrollToBottomRequested;

        public async void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                await LeaveCurrentChatAsync();

                if (_hubConnection != null)
                {
                    await _hubConnection.DisposeAsync();
                }
            }
        }
    }
}