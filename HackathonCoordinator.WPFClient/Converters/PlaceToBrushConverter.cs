using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class PlaceToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int place)
            {
                return place switch
                {
                    1 => new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // Золотой
                    2 => new SolidColorBrush(Color.FromRgb(192, 192, 192)), // Серебряный
                    3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)),  // Бронзовый
                    _ => new SolidColorBrush(Color.FromRgb(100, 100, 100))  // Серый
                };
            }
            return new SolidColorBrush(Color.FromRgb(100, 100, 100));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}