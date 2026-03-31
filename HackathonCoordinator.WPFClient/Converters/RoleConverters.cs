using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class RoleToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int roleId)
            {
                return roleId switch
                {
                    1 => new SolidColorBrush(Color.FromRgb(156, 39, 176)), // Admin - фиолетовый
                    2 => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Organizer - синий
                    3 => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Captain - зеленый
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))  // Member - серый
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class RoleToManageTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int roleId)
            {
                return roleId switch
                {
                    2 => "Снять права организатора",
                    3 => "Назначить организатором",
                    4 => "Назначить организатором",
                    _ => "Управление ролью"
                };
            }
            return "Управление ролью";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}