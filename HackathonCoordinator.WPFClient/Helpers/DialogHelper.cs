using HackathonCoordinator.WPFClient.UserControls;
using System.Windows;

namespace HackathonCoordinator.WPFClient.Helpers
{
    public static class DialogHelper
    {
        public static async Task<bool> ShowConfirmationAsync(string message, string title = "Подтверждение")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return DialogWindow.ShowConfirmation(message, title);
            });
        }

        public static async Task<bool?> ShowYesNoCancelAsync(string message, string title = "Подтверждение")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return DialogWindow.ShowYesNoCancel(message, title);
            });
        }

        public static async Task ShowInfoAsync(string message, string title = "Информация")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DialogWindow.Show(message, title, DialogType.Info);
            });
        }

        public static async Task ShowSuccessAsync(string message, string title = "Успешно")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DialogWindow.Show(message, title, DialogType.Success);
            });
        }

        public static async Task ShowWarningAsync(string message, string title = "Предупреждение")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DialogWindow.Show(message, title, DialogType.Warning);
            });
        }

        public static async Task ShowErrorAsync(string message, string title = "Ошибка")
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DialogWindow.Show(message, title, DialogType.Error);
            });
        }

        // Синхронные версии для использования в не-async методах
        public static bool ShowConfirmationSync(string message, string title = "Подтверждение")
        {
            return DialogWindow.ShowConfirmation(message, title);
        }

        public static void ShowErrorSync(string message, string title = "Ошибка")
        {
            DialogWindow.Show(message, title, DialogType.Error);
        }

        public static void ShowSuccessSync(string message, string title = "Успешно")
        {
            DialogWindow.Show(message, title, DialogType.Success);
        }

        public static async Task<string> ShowInputDialogAsync(string title, string prompt, string defaultValue = "")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return InputDialogWindow.Show(title, prompt, defaultValue);
            });
        }

        public static string ShowInputDialogSync(string title, string prompt, string defaultValue = "")
        {
            return InputDialogWindow.Show(title, prompt, defaultValue);
        }
    }
}