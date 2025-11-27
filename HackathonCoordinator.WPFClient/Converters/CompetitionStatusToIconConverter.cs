using System.Globalization;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class CompetitionStatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusColor)
            {
                return statusColor switch
                {
                    "Green" => "🏃",  // Активно
                    "Orange" => "⏰", // Ожидается
                    "Gray" => "✅",   // Завершено
                    _ => "❓"         // Неизвестно
                };
            }
            return "❓";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
