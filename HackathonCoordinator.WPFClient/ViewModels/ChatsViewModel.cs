using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class ChatsViewModel : BaseViewModel
    {
        public bool doDispose = true;
        private bool _isInitialized = false;

        private readonly ChatService _chatService;
        private readonly TeamService _teamService;
        private readonly TaskService _taskService;
        private readonly UserService _userService;

        public ObservableCollection<ChatListItemDto> Chats { get; } = new();

        public ICommand OpenChatCommand { get; }
        public ICommand RefreshCommand { get; }

        public bool HasNoChats => !Chats.Any();

        public ChatsViewModel()
        {
            _chatService = new ChatService();
            _teamService = new TeamService();
            _taskService = new TaskService();
            _userService = new UserService();

            OpenChatCommand = new AsyncRelayCommand<ChatListItemDto>(
                execute: async (chat) => await OpenChatAsync(chat),
                canExecute: (chat) => chat != null);

            RefreshCommand = new AsyncRelayCommand(
                execute: async () => await RefreshAsync(),
                canExecute: () => true);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await LoadChatsAsync();
            _isInitialized = true;
        }

        public async Task RefreshAsync()
        {
            await LoadChatsAsync();
        }

        private async Task LoadChatsAsync()
        {
            IsLoading = true;

            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Chats.Clear();
                });

                var user = await _userService.GetCurrentUserAsync();
                if (!user.Success)
                {
                    await ShowErrorAsync("Ошибка загрузки пользователя");
                    return;
                }

                // Загружаем чат команды
                if (user.Data.TeamId.HasValue)
                {
                    var teamChat = await _chatService.GetTeamChatAsync(user.Data.TeamId.Value);
                    if (teamChat.Success && teamChat.Data != null)
                    {
                        var chatItem = MapToChatListItem(teamChat.Data, "👥 Чат команды");
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Chats.Add(chatItem);
                        });
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
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                Chats.Add(chatItem);
                            });
                        }
                    }
                }

                // Сортировка чатов
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var sortedChats = Chats
                        .OrderByDescending(c => c.LastMessageTime)
                        .ThenBy(c => c.ChatType == "team" ? 0 : 1)
                        .ToList();

                    Chats.Clear();
                    foreach (var chat in sortedChats)
                    {
                        Chats.Add(chat);
                    }

                    OnPropertyChanged(nameof(HasNoChats));
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки чатов: {ex.Message}");
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
            try
            {
                doDispose = false;

                if (chatItem.TeamId.HasValue)
                {
                    var chat = await _chatService.GetTeamChatAsync(chatItem.TeamId.Value);
                    if (chat.Success)
                    {
                        var chatPage = new ChatPage(chat.Data, true);
                        _navigationService.NavigateTo(chatPage);
                    }
                    else
                    {
                        await ShowErrorAsync($"Не удалось открыть чат команды:\n{chat.Message}");
                        doDispose = true;
                    }
                }
                else if (chatItem.TaskId.HasValue)
                {
                    var chat = await _chatService.GetTaskChatAsync(chatItem.TaskId.Value);
                    if (chat.Success)
                    {
                        var chatPage = new ChatPage(chat.Data, false);
                        _navigationService.NavigateTo(chatPage);
                    }
                    else
                    {
                        await ShowErrorAsync($"Не удалось открыть чат задачи:\n{chat.Message}");
                        doDispose = true;
                    }
                }
                else
                {
                    await ShowErrorAsync("Не удалось определить тип чата");
                    doDispose = true;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка открытия чата: {ex.Message}");
                doDispose = true;
            }
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose)
                return;

            base.DisposeManagedResources();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Chats?.Clear();
            });

            if (_chatService is IDisposable chatDisposable)
                chatDisposable.Dispose();

            if (_teamService is IDisposable teamDisposable)
                teamDisposable.Dispose();

            if (_taskService is IDisposable taskDisposable)
                taskDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();
        }
    }

    public class ChatListItemDto : BaseViewModel
    {
        private int _chatId;
        public int ChatId
        {
            get => _chatId;
            set => SetProperty(ref _chatId, value);
        }

        private string _chatType;
        public string ChatType
        {
            get => _chatType;
            set => SetProperty(ref _chatType, value);
        }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        private int _participantsCount;
        public int ParticipantsCount
        {
            get => _participantsCount;
            set => SetProperty(ref _participantsCount, value);
        }

        private string _lastMessage;
        public string LastMessage
        {
            get => _lastMessage;
            set
            {
                SetProperty(ref _lastMessage, value);
                OnPropertyChanged(nameof(LastMessagePreview));
            }
        }

        private DateTime _lastMessageTime;
        public DateTime LastMessageTime
        {
            get => _lastMessageTime;
            set
            {
                SetProperty(ref _lastMessageTime, value);
                OnPropertyChanged(nameof(TimeAgo));
            }
        }

        private string _lastMessageSender;
        public string LastMessageSender
        {
            get => _lastMessageSender;
            set
            {
                SetProperty(ref _lastMessageSender, value);
                OnPropertyChanged(nameof(LastMessagePreview));
            }
        }

        private int? _teamId;
        public int? TeamId
        {
            get => _teamId;
            set => SetProperty(ref _teamId, value);
        }

        private int? _taskId;
        public int? TaskId
        {
            get => _taskId;
            set => SetProperty(ref _taskId, value);
        }

        public string ParticipantsText => $"{ParticipantsCount} участников";

        public string LastMessagePreview
        {
            get
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
        }

        public string TimeAgo
        {
            get
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
}