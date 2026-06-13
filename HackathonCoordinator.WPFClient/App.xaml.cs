using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using System.Windows;

namespace HackathonCoordinator.WPFClient
{
    public partial class App : Application
    {
        public static NavigationService NavigationService { get; private set; }
        public static string CurrentTheme { get; private set; } = "Light";

        public static event EventHandler<string> ThemeChanged;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            NavigationService = new NavigationService();
            _ = SwitchThemeAsync("Light");
        }

        public static async Task SwitchThemeAsync(string themeName)
        {
            try
            {
                CurrentTheme = themeName;
                var themePath = $"Themes/{themeName}Theme.xaml";

                // Проверяем, что словари существуют
                if (Current.Resources.MergedDictionaries.Count > 1)
                {
                    // Заменяем тему (второй словарь)
                    Current.Resources.MergedDictionaries[1] =
                        new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };
                }
                else
                {
                    // Добавляем тему, если словарей мало
                    Current.Resources.MergedDictionaries.Add(
                        new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) });
                }

                ThemeChanged?.Invoke(null, themeName);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync($"Ошибка загрузки темы {themeName}: {ex.Message}");
                // Пробуем загрузить светлую тему как запасной вариант
                try
                {
                    Current.Resources.MergedDictionaries[1] =
                        new ResourceDictionary { Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative) };
                }
                catch
                {
                    await DialogHelper.ShowErrorAsync($"Ошибка загрузки темы LightTheme: {ex.Message}");
                }
            }
        }

        // Быстрые методы переключения
        public static async Task SwitchToLight() => await SwitchThemeAsync("Light");
        public static async Task SwitchToDark() => await SwitchThemeAsync("Dark");
        public static async Task SwitchToSummer() => await SwitchThemeAsync("Summer");
        public static async Task SwitchToWinter() => await SwitchThemeAsync("Winter");
        public static async Task SwitchToAutumn() => await SwitchThemeAsync("Autumn");
        public static async Task SwitchToSpring() => await SwitchThemeAsync("Spring");

        // Циклическое переключение между всеми темами
        public static async Task CycleThemes()
        {
            var themes = new[] { "Light", "Dark", "Summer", "Winter", "Autumn", "Spring" };
            var currentIndex = Array.IndexOf(themes, CurrentTheme);
            var nextIndex = (currentIndex + 1) % themes.Length;
            await SwitchThemeAsync(themes[nextIndex]);
        }
    }
}