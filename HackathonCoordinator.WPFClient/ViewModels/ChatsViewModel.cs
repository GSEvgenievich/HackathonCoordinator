using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class ChatsViewModel : INotifyPropertyChanged
    {
        private readonly ChatService _chatService;
        private readonly TeamService _teamService;
        private readonly TaskService _taskService;
        private readonly UserService _userService;
        private readonly NavigationService _navigationService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ChatListItemDto> Chats { get; } = new();

        public ICommand LoadChatsCommand { get; }
        public ICommand OpenChatCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }

        public ChatsViewModel()
        {
            _chatService = new ChatService();
            _teamService = new TeamService();
            _taskService = new TaskService();
            _userService = new UserService();
            _navigationService = App.NavigationService;

            LoadChatsCommand = new RelayCommand(async () => await LoadChatsAsync());
            OpenChatCommand = new RelayCommand<ChatListItemDto>(async (chat) => await OpenChatAsync(chat));
            BackCommand = new RelayCommand(GoBack);
            RefreshCommand = new RelayCommand(async () => await LoadChatsAsync());

            // Загружаем чаты при создании
            LoadChatsCommand.Execute(null);
        }

        private async Task LoadChatsAsync()
        {
            IsLoading = true;

            try
            {
                Chats.Clear();

                // Получаем текущего пользователя
                var user = await _userService.GetCurrentUserAsync();
                if (!user.Success)
                {
                    MessageBox.Show("Ошибка загрузки пользователя");
                    return;
                }

                // Загружаем чат команды
                if (user.Data.TeamId.HasValue)
                {
                    var teamChat = await _chatService.GetTeamChatAsync(user.Data.TeamId.Value);
                    if (teamChat.Success && teamChat.Data != null)
                    {
                        var chatItem = MapToChatListItem(teamChat.Data, "👥 Чат команды");
                        Chats.Add(chatItem);
                    }
                }

                // Загружаем чаты задач
                var userTasksIds = await _taskService.GetUserTasksIdsAsync();
                if (userTasksIds.Success)
                {
                    foreach (var taskId in userTasksIds.Data)
                    {
                        var taskChat = await _chatService.GetTaskChatAsync(taskId);
                        if (taskChat.Success && taskChat.Data != null)
                        {
                            var chatItem = MapToChatListItem(taskChat.Data, $"🎯 {taskChat.Data.Name}");
                            Chats.Add(chatItem);
                        }
                    }
                }

                // Сортируем по времени последнего сообщения (сверху самые активные)
                var sortedChats = Chats
                    .OrderByDescending(c => c.LastMessageTime)
                    .ThenBy(c => c.ChatType == "team" ? 0 : 1) // Сначала чат команды
                    .ToList();

                Chats.Clear();
                foreach (var chat in sortedChats)
                {
                    Chats.Add(chat);
                }

                OnPropertyChanged(nameof(HasNoChats));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки чатов: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private ChatListItemDto MapToChatListItem(ChatDto chat, string displayName)
        {
            var lastMessage = chat.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

            return new ChatListItemDto
            {
                ChatId = chat.Id,
                ChatType = chat.Type.ToLower(),
                DisplayName = displayName,
                ParticipantsCount = chat.Participants.Count,
                LastMessage = lastMessage?.Text ?? "Нет сообщений",
                LastMessageTime = lastMessage?.SentAt ?? chat.CreatedAt,
                LastMessageSender = lastMessage?.UserName,
                TeamId = chat.TeamId,
                TaskId = chat.TaskId
            };
        }

        private async Task OpenChatAsync(ChatListItemDto chatItem)
        {
            if (chatItem == null) return;

            try
            {
                var chatPage = new ChatPage();
                var viewModel = chatPage.DataContext as ChatViewModel;

                if (viewModel != null)
                {
                    if (chatItem.ChatType == "чат команды" && chatItem.TeamId.HasValue)
                    {
                        await viewModel.LoadTeamChatAsync(chatItem.TeamId.Value);
                    }
                    else
                    {
                        await viewModel.LoadTaskChatAsync(chatItem.TaskId.Value);
                    }

                    _navigationService.NavigateTo(chatPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия чата: {ex.Message}");
            }
        }

        private void GoBack()
        {
            _navigationService.GoBack();
        }

        public bool HasNoChats => !Chats.Any();
    }

    public class ChatListItemDto
    {
        public int ChatId { get; set; }
        public string ChatType { get; set; }
        public string DisplayName { get; set; }
        public int ParticipantsCount { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public string LastMessageSender { get; set; }
        public int? TeamId { get; set; }
        public int? TaskId { get; set; }

        public string ParticipantsText => $"{ParticipantsCount} участников";
        public string LastMessagePreview => GetMessagePreview();
        public string TimeAgo => GetTimeAgo();

        private string GetMessagePreview()
        {
            if (string.IsNullOrEmpty(LastMessage) || LastMessage == "Нет сообщений")
                return "Нет сообщений";

            var preview = LastMessage.Length > 50
                ? LastMessage.Substring(0, 50) + "..."
                : LastMessage;

            if (!string.IsNullOrEmpty(LastMessageSender))
            {
                return $"{LastMessageSender}: {preview}";
            }

            return preview;
        }

        private string GetTimeAgo()
        {
            var timeSpan = DateTime.Now - LastMessageTime;

            if (timeSpan.TotalMinutes < 1)
                return "только что";
            if (timeSpan.TotalHours < 1)
                return $"{(int)timeSpan.TotalMinutes} мин назад";
            if (timeSpan.TotalDays < 1)
                return $"{(int)timeSpan.TotalHours} ч назад";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} дн назад";

            return LastMessageTime.ToString("dd.MM.yy");
        }
    }
}