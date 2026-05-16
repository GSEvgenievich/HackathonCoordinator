using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class BoolToMessageStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Возвращаем строку с именем ресурса, а не сам объект
            return (value is bool isMyMessage && isMyMessage) ? "MyMessageStyle" : "OtherMessageStyle";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}