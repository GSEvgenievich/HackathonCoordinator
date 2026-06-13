using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class CompetitionStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusColor)
            {
                return statusColor switch
                {
                    "Green" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),   // Зеленый - активно
                    "Orange" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),  // Оранжевый - ожидается
                    "Gray" => new SolidColorBrush(Color.FromRgb(108, 117, 125)),  // Серый - завершено
                    "Archive" => new SolidColorBrush(Color.FromRgb(106, 90, 205)), // Сине-фиолетовый - в архиве
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
                };
            }
            return new SolidColorBrush(Color.FromRgb(108, 117, 125));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}