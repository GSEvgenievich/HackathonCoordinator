using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HackathonCoordinator.WPFClient.Views
{
    /// <summary>
    /// Логика взаимодействия для EditCompetitionPage.xaml
    /// </summary>
    public partial class EditCompetitionPage : Page
    {
        private static CompetitionDto? Competition {  get; set; }

        public EditCompetitionPage(CompetitionDto competition)
        {
            InitializeComponent();
            Competition = competition;
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is EditCompetitionViewModel viewModel)
            {
                if (Competition != null)
                {
                    viewModel.LoadCompetitionData(Competition);
                }
            }
        }
    }
}
