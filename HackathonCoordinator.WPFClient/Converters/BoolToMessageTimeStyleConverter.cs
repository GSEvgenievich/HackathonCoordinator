using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class BoolToMessageTimeStyleConverter : IValueConverter
    {
        public static BoolToMessageTimeStyleConverter Instance { get; } = new BoolToMessageTimeStyleConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                // Для своих сообщений - светло-серый, для чужих - темно-серый
                return isMyMessage ? new SolidColorBrush(Color.FromRgb(220, 220, 220)) : new SolidColorBrush(Color.FromRgb(120, 120, 120));
            }
            return new SolidColorBrush(Color.FromRgb(120, 120, 120));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}