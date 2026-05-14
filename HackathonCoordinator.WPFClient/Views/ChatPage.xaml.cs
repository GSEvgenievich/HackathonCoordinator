using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class ChatPage : Page
    {
        private ChatDto _chat;
        private bool _isTeamChat;

        public ChatPage()
        {
            InitializeComponent();
        }

        public ChatPage(ChatDto chat, bool isTeamChat) : this()
        {
            _chat = chat;
            _isTeamChat = isTeamChat;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChatViewModel viewModel)
            {
                viewModel.doDispose = true;
                viewModel.RequestFocus += OnRequestFocus;
                viewModel.ScrollToBottomRequested += ViewModel_ScrollToBottomRequested;
                await viewModel.InitializeAsync(_chat, _isTeamChat);
            }
            ScrollToBottom();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChatViewModel viewModel)
            {
                viewModel.ScrollToBottomRequested -= ViewModel_ScrollToBottomRequested;
                if (viewModel.doDispose)
                {
                    viewModel.Dispose();
                }
            }
        }

        private void OnRequestFocus()
        {
            // Устанавливаем фокус на поле ввода
            Dispatcher.BeginInvoke(() =>
            {
                if (MessageTextBox != null)
                {
                    MessageTextBox.Focus();
                    // Устанавливаем курсор в конец текста
                    MessageTextBox.CaretIndex = MessageTextBox.Text.Length;
                }
            });
        }

        private void CopyAttachmentName_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var attachment = menuItem?.DataContext as ServiceLayer.DTOs.MessageAttachmentDto;

            if (attachment != null)
            {
                Clipboard.SetText(attachment.FileName);
                ShowTemporaryTooltip("Название файла скопировано");
            }
        }

        private void ShowTemporaryTooltip(string message)
        {
            var tooltip = new ToolTip
            {
                Content = message,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
                IsOpen = true,
                HorizontalOffset = 10,
                VerticalOffset = -20
            };

            var timer = new System.Timers.Timer(1500);
            timer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() => tooltip.IsOpen = false);
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void ViewModel_ScrollToBottomRequested(object sender, System.EventArgs e)
        {
            // Прокручиваем к низу когда ViewModel запрашивает
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            // Автопрокрутка к низу при новых сообщениях
            if (MessagesScrollViewer != null)
            {
                // Небольшая задержка чтобы UI успел обновиться
                Dispatcher.BeginInvoke(() =>
                {
                    MessagesScrollViewer.ScrollToEnd();
                });
            }
        }

        private void MessagesScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Автоматическая прокрутка при добавлении новых сообщений
            if (e.ExtentHeightChange > 0)
            {
                ScrollToBottom();
            }
        }

        private void CopyMessageText_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var message = menuItem?.DataContext as ServiceLayer.DTOs.MessageDto;

            if (message != null && !string.IsNullOrEmpty(message.Text))
            {
                Clipboard.SetText(message.Text);
                ShowTemporaryTooltip("Текст скопирован");
            }
        }
    }
}