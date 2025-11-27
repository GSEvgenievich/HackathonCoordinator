using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class MyTaskBorderConverter : IValueConverter
    {
        public static MyTaskBorderConverter Instance { get; } = new MyTaskBorderConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyTask)
            {
                return isMyTask 
                    ? Application.Current.FindResource("MyTaskCardStyle") 
                    : Application.Current.FindResource("TaskCardStyle");
            }
            return Application.Current.FindResource("TaskCardStyle");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}