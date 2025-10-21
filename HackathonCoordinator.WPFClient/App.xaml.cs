using HackathonCoordinator.WPFClient.Services;
using System.Windows;

namespace HackathonCoordinator.WPFClient
{
    public partial class App : Application
    {
        public static NavigationService NavigationService { get; private set; }
        public static bool IsDarkTheme { get; private set; } = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            NavigationService = new NavigationService();
            SwitchToLightTheme();
        }
        public static void SwitchToLightTheme()
        {
            IsDarkTheme = false;
            Current.Resources.MergedDictionaries[0] =
                new ResourceDictionary { Source = new System.Uri("Themes/LightTheme.xaml", System.UriKind.Relative) };
        }

        public static void SwitchToDarkTheme()
        {
            IsDarkTheme = true;
            Current.Resources.MergedDictionaries[0] =
                new ResourceDictionary { Source = new System.Uri("Themes/DarkTheme.xaml", System.UriKind.Relative) };
        }

        public static void ToggleTheme()
        {
            if (IsDarkTheme)
                SwitchToLightTheme();
            else
                SwitchToDarkTheme();
        }
    }
}