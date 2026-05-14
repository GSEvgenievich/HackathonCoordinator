using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class BoolToMessageTimeColorConverter : IValueConverter
    {
        public static BoolToMessageTimeColorConverter Instance { get; } = new BoolToMessageTimeColorConverter();

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