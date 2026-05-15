using HackathonCoordinator.ServiceLayer;
using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.Storages;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class ChatViewModel : BaseViewModel
    {
        public bool doDispose = true;

        private readonly ChatService _chatService;
        private readonly UserService _userService;

        private HubConnection _hubConnection;
        private ChatDto _currentChat;
        private UserDto _currentUser;

        private bool _isConnected;
        private bool _isCaptain;
        private bool _isOrganizer;
        private bool _isLoading;
        private string _chatTitle = "";
        private string _newMessageText = "";
        private bool _isInitialized = false;

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
            set
            {
                SetProperty(ref _isCaptain, value);
                OnPropertyChanged(nameof(CanNotify));
            }
        }

        public bool IsOrganizer
        {
            get => _isOrganizer;
            set
            {
                SetProperty(ref _isOrganizer, value);
                OnPropertyChanged(nameof(CanNotify));
            }
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

        private ObservableCollection<FileUploadData> _selectedFiles = new();
        public ObservableCollection<FileUploadData> SelectedFiles
        {
            get => _selectedFiles;
            set
            {
                SetProperty(ref _selectedFiles, value);
                OnPropertyChanged(nameof(HasFiles));
                OnPropertyChanged(nameof(FilesPreview));
            }
        }

        public bool HasFiles => SelectedFiles?.Any() == true;
        public string FilesPreview => HasFiles ? $"📎 {SelectedFiles.Count} файл(ов)" : "";
        public bool CanSendMessage => !string.IsNullOrWhiteSpace(NewMessageText) && !IsLoading && _isConnected;
        public bool HasNoMessages => !Messages.Any();
        public bool CanNotify => IsCaptain || IsOrganizer;

        public ObservableCollection<MessageDto> Messages { get; } = new();
        public ObservableCollection<ChatParticipantDto> Participants { get; } = new();

        public ICommand SendMessageCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand EditMessageCommand { get; }
        public ICommand DeleteMessageCommand { get; }
        public ICommand AddFilesCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand HandleAttachmentClickCommand { get; }
        public ICommand ViewImageCommand { get; }
        public ICommand DownloadAttachmentCommand { get; }
        public ICommand ViewParticipantProfileCommand { get; }
        public ICommand InsertNotifyCommand { get; }

        public event EventHandler ScrollToBottomRequested;
        public event Action RequestFocus;

        public ChatViewModel()
        {
            _chatService = new ChatService();
            _userService = new UserService();

            SendMessageCommand = new AsyncRelayCommand(
                execute: async () => await SendMessageAsync(),
                canExecute: () => CanSendMessage && _isConnected);

            BackCommand = new RelayCommand(GoBack);
            EditMessageCommand = new AsyncRelayCommand<MessageDto>(
                execute: async (msg) => await EditMessageAsync(msg),
                canExecute: (msg) => msg != null && msg.IsMyMessage);

            DeleteMessageCommand = new AsyncRelayCommand<MessageDto>(
                execute: async (msg) => await DeleteMessageAsync(msg),
                canExecute: (msg) => msg != null && msg.IsMyMessage);

            HandleAttachmentClickCommand = new AsyncRelayCommand<MessageAttachmentDto>(
                execute: async (attachment) => await HandleAttachmentClickAsync(attachment),
                canExecute: (attachment) => attachment != null);

            ViewParticipantProfileCommand = new AsyncRelayCommand<ChatParticipantDto>(
                execute: async (participant) => await ExecuteViewParticipantProfileAsync(participant),
                canExecute: (participant) => participant != null);

            ViewImageCommand = new AsyncRelayCommand<MessageAttachmentDto>(
                execute: async (attachment) => await ViewImageAsync(attachment),
                canExecute: (attachment) => attachment != null && attachment.IsImage);

            DownloadAttachmentCommand = new AsyncRelayCommand<MessageAttachmentDto>(
                execute: async (attachment) => await DownloadAttachmentAsync(attachment),
                canExecute: (attachment) => attachment != null);

            AddFilesCommand = new RelayCommand(AddFiles);
            RemoveFileCommand = new RelayCommand<FileUploadData>(RemoveFile);
            InsertNotifyCommand = new RelayCommand(InsertNotify);

        }

        public async Task InitializeAsync(ChatDto chat, bool isTeamChat)
        {
            if (_isInitialized) return;

            IsLoading = true;

            try
            {
                CurrentChat = chat;

                if (isTeamChat)
                {
                    ChatTitle = "💬 Чат команды";
                }
                else
                {
                    ChatTitle = "💬 Чат задачи";
                }

                await LoadCurrentUser();
                await UpdateMessagesAndParticipantsAsync();

                await InitializeSignalRAsync();

                _isInitialized = true;
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

        private async Task ExecuteViewParticipantProfileAsync(ChatParticipantDto participant)
        {
            if (participant == null) return;

            try
            {
                doDispose = false;
                var profilePage = new ProfilePage(participant.UserId);
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

        private async Task LoadCurrentUser()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();

                if (user.Success)
                {
                    CurrentUser = user.Data;
                }

                if (CurrentUser != null)
                {
                    IsCaptain = CurrentUser.RoleId == (int)Roles.Captain;
                    IsOrganizer = CurrentUser.RoleId == (int)Roles.Organizer;
                }
                else
                {
                    IsCaptain = false;
                    IsOrganizer = false;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки пользователя: {ex.Message}");
            }
        }

        private async Task InitializeSignalRAsync()
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

        private void InsertNotify()
        {
            var currentText = NewMessageText;

            if (!currentText.Contains("@notify"))
            {
                NewMessageText = "@notify " + currentText;
            }

            RequestFocus?.Invoke();
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

        private async Task HandleAttachmentClickAsync(MessageAttachmentDto attachment)
        {
            if (attachment.IsImage)
            {
                await ViewImageAsync(attachment);
            }
            else
            {
                await DownloadAttachmentAsync(attachment);
            }
        }

        private async Task ViewImageAsync(MessageAttachmentDto attachment)
        {
            try
            {
                // Показываем индикатор загрузки
                IsLoading = true;

                var result = await _chatService.GetFullImageAsync(attachment.Id);

                if (result.Success && result.Data != null && result.Data.Length > 0)
                {
                    var previewWindow = new ImagePreviewWindow(result.Data, attachment.FileName);
                    previewWindow.Owner = Application.Current.MainWindow;
                    previewWindow.ShowDialog();
                }
                else
                {
                    await ShowErrorAsync("Не удалось загрузить изображение");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка просмотра: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DownloadAttachmentAsync(MessageAttachmentDto attachment)
        {
            try
            {
                IsLoading = true;

                var result = await _chatService.DownloadAttachmentAsync(attachment.Id);

                if (!result.Success)
                {
                    await ShowErrorAsync(result.Message);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = attachment.FileName,
                    Title = "Сохранить файл как...",
                    Filter = "Все файлы|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(saveFileDialog.FileName, result.Data);
                    await ShowSuccessAsync("Файл успешно сохранен");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка скачивания: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void AddFiles()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите файлы",
                Multiselect = true,
                Filter = "Все файлы|*.*|Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Документы|*.pdf;*.doc;*.docx;*.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    try
                    {
                        var fileInfo = new FileInfo(fileName);
                        var fileData = await File.ReadAllBytesAsync(fileName);

                        SelectedFiles.Add(new FileUploadData
                        {
                            FileName = Path.GetFileName(fileName),
                            ContentType = GetContentType(fileName),
                            Data = fileData,
                            Length = fileInfo.Length
                        });
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorAsync($"Ошибка загрузки файла {Path.GetFileName(fileName)}: {ex.Message}");
                    }
                }
                OnPropertyChanged(nameof(HasFiles));
                OnPropertyChanged(nameof(FilesPreview));
            }
        }

        private void RemoveFile(FileUploadData file)
        {
            SelectedFiles.Remove(file);
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(FilesPreview));
        }

        private string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private async Task SendMessageAsync()
        {
            if (!CanSendMessage && !HasFiles) return;

            var messageText = NewMessageText.Trim();
            NewMessageText = "";

            try
            {
                ApiResponse<MessageDto> result;

                if (HasFiles)
                {
                    result = await _chatService.SendMessageWithAttachmentsAsync(CurrentChat.Id, messageText, SelectedFiles.ToList());
                    SelectedFiles.Clear();
                    OnPropertyChanged(nameof(HasFiles));
                    OnPropertyChanged(nameof(FilesPreview));
                }
                else
                {
                    result = await _chatService.SendMessageAsync(CurrentChat.Id, messageText);
                }

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
            var result = await ShowYesNoCancelAsync("Вы уверены, что хотите удалить это сообщение?", "Подтверждение удаления");

            if (result == true)
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

            _navigationService.GoBack();
        }

        private void ScrollToBottom()
        {
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
        }

        private async Task ShowSuccessAsync(string message)
        {
            await ShowInfoAsync(message, "Успешно");
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose)
                return;

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