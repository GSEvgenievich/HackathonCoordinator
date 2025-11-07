using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class EditProjectPage : Page
    {
        private static ProjectDto Project { get; set; }
        private static int TeamId { get; set; }

        public EditProjectPage(ProjectDto project)
        {
            InitializeComponent();
            Project = project;
            Loaded += OnPageLoaded;
        }

        public EditProjectPage(int teamId)
        {
            InitializeComponent();
            TeamId = teamId;
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is EditProjectViewModel viewModel)
            {
                if (Project != null)
                {
                    viewModel.LoadProjectData(Project);
                }
                else
                {
                    viewModel.InitializeForCreate(TeamId);
                }
            }
        }
    }
}