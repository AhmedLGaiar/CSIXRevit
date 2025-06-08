using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StructLink_X.Converters
{
    /// <summary>
    /// Converts ViewMode enum values to Visibility based on converter parameter
    /// </summary>
    public class ViewModeToVisibilityConverter : IValueConverter
    {
        public static readonly ViewModeToVisibilityConverter Instance = new ViewModeToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ViewMode viewMode) || !(parameter is string param))
                return Visibility.Collapsed;

            switch (param)
            {
                case "View2D":
                    return viewMode == ViewMode.View2D ? Visibility.Visible : Visibility.Collapsed;

                case "View3D":
                    return viewMode == ViewMode.View3D ? Visibility.Visible : Visibility.Collapsed;

                case "Both":
                    return viewMode == ViewMode.Both ? Visibility.Visible : Visibility.Collapsed;

                case "Single":
                    // Show single view container when mode is View2D or View3D (not Both)
                    return (viewMode == ViewMode.View2D || viewMode == ViewMode.View3D)
                        ? Visibility.Visible : Visibility.Collapsed;

                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

