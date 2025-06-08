using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StructLink_X.Converters
{
 
    public enum ViewMode
    {
        View2D,
        View3D,
        Both
    }

    /// <summary>
    /// Converts null values to inverse Visibility (null = Collapsed, not null = Visible)
    /// </summary>
    public class NotNullToVisibilityConverter : IValueConverter
    {
        public static readonly NotNullToVisibilityConverter Instance = new NotNullToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Show when value exists, hide when value is null
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

