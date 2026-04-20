using HackathonCoordinator.WPFClient.Services;
using System;
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
            SwitchTheme("Light");
        }

        public static void SwitchTheme(string themeName)
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
                MessageBox.Show($"Ошибка загрузки темы {themeName}: {ex.Message}");
                // Пробуем загрузить светлую тему как запасной вариант
                try
                {
                    Current.Resources.MergedDictionaries[1] =
                        new ResourceDictionary { Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative) };
                }
                catch
                {
                    MessageBox.Show($"Ошибка загрузки темы LightTheme: {ex.Message}");
                }
            }
        }

        // Быстрые методы переключения
        public static void SwitchToLight() => SwitchTheme("Light");
        public static void SwitchToDark() => SwitchTheme("Dark");
        public static void SwitchToSummer() => SwitchTheme("Summer");
        public static void SwitchToWinter() => SwitchTheme("Winter");
        public static void SwitchToAutumn() => SwitchTheme("Autumn");
        public static void SwitchToSpring() => SwitchTheme("Spring");

        // Циклическое переключение между всеми темами
        public static void CycleThemes()
        {
            var themes = new[] { "Light", "Dark", "Summer", "Winter", "Autumn", "Spring" };
            var currentIndex = Array.IndexOf(themes, CurrentTheme);
            var nextIndex = (currentIndex + 1) % themes.Length;
            SwitchTheme(themes[nextIndex]);
        }
    }
}