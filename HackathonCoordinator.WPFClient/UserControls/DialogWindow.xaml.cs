using System.Windows;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.UserControls
{
    public enum DialogType
    {
        Info,
        Success,
        Warning,
        Error,
        Question
    }

    public enum CustomDialogResult
    {
        OK,
        Yes,
        No
    }

    public partial class DialogWindow : Window
    {
        private CustomDialogResult _result = CustomDialogResult.No;

        public DialogWindow()
        {
            InitializeComponent();

            // Настройка кнопок в конструкторе
            OkButton.Click += OkButton_Click;
            YesButton.Click += YesButton_Click;
            NoButton.Click += NoButton_Click;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomDialogResult.OK;
            Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomDialogResult.Yes;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            _result = CustomDialogResult.No;
            Close();
        }

        public static CustomDialogResult Show(
            string message,
            string title = "",
            DialogType type = DialogType.Info,
            bool showYesNo = false)
        {
            var dialog = new DialogWindow();

            // Устанавливаем сообщение
            dialog.MessageText.Text = message;

            // Устанавливаем заголовок и иконку
            switch (type)
            {
                case DialogType.Info:
                    dialog.TitleText.Text = string.IsNullOrEmpty(title) ? "Информация" : title;
                    dialog.IconText.Text = "ℹ️";
                    dialog.IconText.Foreground = (Brush)Application.Current.FindResource("PrimaryBrush");
                    break;
                case DialogType.Success:
                    dialog.TitleText.Text = string.IsNullOrEmpty(title) ? "Успешно" : title;
                    dialog.IconText.Text = "✅";
                    dialog.IconText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    break;
                case DialogType.Warning:
                    dialog.TitleText.Text = string.IsNullOrEmpty(title) ? "Предупреждение" : title;
                    dialog.IconText.Text = "⚠️";
                    dialog.IconText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    break;
                case DialogType.Error:
                    dialog.TitleText.Text = string.IsNullOrEmpty(title) ? "Ошибка" : title;
                    dialog.IconText.Text = "❌";
                    dialog.IconText.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    break;
                case DialogType.Question:
                    dialog.TitleText.Text = string.IsNullOrEmpty(title) ? "Подтверждение" : title;
                    dialog.IconText.Text = "❓";
                    dialog.IconText.Foreground = (Brush)Application.Current.FindResource("PrimaryBrush");
                    break;
            }

            // Настраиваем кнопки
            if (showYesNo)
            {
                dialog.OkButton.Visibility = Visibility.Collapsed;
                dialog.YesButton.Visibility = Visibility.Visible;
                dialog.NoButton.Visibility = Visibility.Visible;
            }
            else
            {
                dialog.OkButton.Visibility = Visibility.Visible;
                dialog.YesButton.Visibility = Visibility.Collapsed;
                dialog.NoButton.Visibility = Visibility.Collapsed;
            }

            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();

            return dialog._result;
        }

        public static bool ShowConfirmation(string message, string title = "Подтверждение")
        {
            var result = Show(message, title, DialogType.Question, true);
            return result == CustomDialogResult.Yes;
        }

        public static bool? ShowYesNoCancel(string message, string title = "Подтверждение")
        {
            var result = Show(message, title, DialogType.Question, true);
            return result switch
            {
                CustomDialogResult.Yes => true,
                CustomDialogResult.No => false,
                _ => null
            };
        }
    }
}