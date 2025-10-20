using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    /// <summary>
    /// Логика взаимодействия для VerifyEmailPage.xaml
    /// </summary>
    public partial class VerifyEmailPage : Page
    {
        public VerifyEmailPage(string email)
        {
            InitializeComponent();
            DataContext = new VerifyEmailViewModel(email);
        }
    }
}
