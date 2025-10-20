using HackathonCoordinator.WPFClient.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace HackathonCoordinator.WPFClient
{
    public partial class App : Application
    {
        public static NavigationService NavigationService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            NavigationService = new NavigationService();
        }
    }
}