using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class EditTaskPage : Page
    {
        private readonly int _id;
        private readonly bool _isCreateMode;

        public EditTaskPage(int id, bool isCreateMode = true)
        {
            InitializeComponent();
            _id = id;
            _isCreateMode = isCreateMode;
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditTaskViewModel viewModel)
            {
                if (_isCreateMode)
                    viewModel.InitializeForCreate(_id);
                else
                    viewModel.InitializeForEdit(_id);

                viewModel.ScrollToBottomRequested += ViewModel_ScrollToBottomRequested;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditTaskViewModel viewModel)
            {
                viewModel.Dispose();
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
            if (MainScrollViewer != null)
            {
                // Небольшая задержка чтобы UI успел обновиться
                Dispatcher.BeginInvoke(() =>
                {
                    MainScrollViewer.ScrollToEnd();
                });
            }
        }
    }
}