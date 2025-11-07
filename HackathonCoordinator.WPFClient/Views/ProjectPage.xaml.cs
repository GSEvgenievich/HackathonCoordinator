using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    /// <summary>
    /// Логика взаимодействия для ProjectPage.xaml
    /// </summary>
    public partial class ProjectPage : Page
    {
        private static ProjectDto? Project { get; set; }

        public ProjectPage(ProjectDto project)
        {
            InitializeComponent();
            Project = project;
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ProjectViewModel viewModel)
            {
                viewModel.LoadProjectData(Project);
            }
        }
    }
}

