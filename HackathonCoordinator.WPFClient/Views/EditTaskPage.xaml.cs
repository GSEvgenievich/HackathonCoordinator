using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class EditTaskPage : Page
    {
        private readonly int _teamId;
        private readonly TaskDetailsDto _task;
        private readonly bool _isCreateMode;

        public EditTaskPage(int teamId, bool isCreateMode = true)
        {
            InitializeComponent();
            _teamId = teamId;
            _isCreateMode = isCreateMode;
            Loaded += OnPageLoaded;
        }

        public EditTaskPage(TaskDetailsDto task, bool isCreateMode = false)
        {
            InitializeComponent();
            _task = task;
            _isCreateMode = isCreateMode;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditTaskViewModel viewModel)
            {
                viewModel.ScrollToBottomRequested += ViewModel_ScrollToBottomRequested;

                if (_isCreateMode && _teamId > 0)
                    await viewModel.InitializeForCreateAsync(_teamId);
                else if (_task != null)
                    await viewModel.InitializeForEditAsync(_task);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditTaskViewModel viewModel)
            {
                viewModel.ScrollToBottomRequested -= ViewModel_ScrollToBottomRequested;
                viewModel.Dispose();
            }
        }

        private void ViewModel_ScrollToBottomRequested(object sender, System.EventArgs e) => ScrollToBottom();

        private void ScrollToBottom()
        {
            if (MainScrollViewer != null)
            {
                Dispatcher.BeginInvoke(() => MainScrollViewer.ScrollToEnd());
            }
        }
    }
}