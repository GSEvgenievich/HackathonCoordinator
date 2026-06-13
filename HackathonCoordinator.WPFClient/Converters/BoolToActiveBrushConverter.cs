// HackathonCoordinator.WPFClient/Converters/StatusToBrushConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            if (status?.Contains("Сейчас идет") == true)
                return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
            if (status?.Contains("Предстоит") == true)
                return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Серый
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToActiveBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
            {
                return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
            }
            return new SolidColorBrush(Color.FromRgb(100, 100, 100)); // Темно-серый
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}