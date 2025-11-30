using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class ChatPage : Page
    {
        public ChatPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Подписываемся на событие прокрутки когда страница загружается
            if (DataContext is ChatViewModel viewModel)
            {
                viewModel.ScrollToBottomRequested += ViewModel_ScrollToBottomRequested;
            }
            ScrollToBottom();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Отписываемся когда страница выгружается
            if (DataContext is ChatViewModel viewModel)
            {
                viewModel.ScrollToBottomRequested -= ViewModel_ScrollToBottomRequested;
            }
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
            if (sender is MenuItem menuItem && menuItem.DataContext is MessageDto message)
            {
                Clipboard.SetText(message.Text);
            }
        }
    }
}