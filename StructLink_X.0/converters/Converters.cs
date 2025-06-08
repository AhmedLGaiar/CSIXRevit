using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StructLink_X.converters
{
 
    public enum ViewMode
    {
        View2D,
        View3D,
        Both
    }

    /// <summary>
    /// Converts boolean values to Visibility enum values
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new BooleanToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts null values to Visibility enum values
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new NullToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Show when value is null, hide when value exists
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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

    /// <summary>
    /// Inverted Boolean to Visibility converter (true = Collapsed, false = Visible)
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public static readonly InverseBooleanToVisibilityConverter Instance = new InverseBooleanToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return true;
        }
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

