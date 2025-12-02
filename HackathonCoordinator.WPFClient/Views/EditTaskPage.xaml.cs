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
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditTaskViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}