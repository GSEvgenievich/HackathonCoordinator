using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class EditCompetitionPage : Page
    {
        private static CompetitionDto _competition;

        public EditCompetitionPage(CompetitionDto competition)
        {
            InitializeComponent();
            _competition = competition;
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditCompetitionViewModel viewModel && _competition != null)
            {
                viewModel.LoadCompetitionData(_competition);
                viewModel.ScrollToBottomRequested += ViewModel_ScrollToBottomRequested;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditCompetitionViewModel viewModel)
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