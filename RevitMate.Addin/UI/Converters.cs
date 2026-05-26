using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RevitMate.Addin.UI
{
    /// <summary>
    /// Aligns chat bubbles: user messages flush right, everything else flush left.
    /// </summary>
    public class RoleToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Role role && role == Role.User
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Fill color for a chat bubble. User = light teal, others = white.
    /// </summary>
    public class RoleToBubbleBrushConverter : IValueConverter
    {
        private static readonly Brush UserBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0xEF, 0xEE));
        private static readonly Brush OtherBrush = Brushes.White;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Role role && role == Role.User ? UserBrush : OtherBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Border color for a chat bubble. AI/system get a soft grey border;
    /// user bubbles get a barely-visible matching border.
    /// </summary>
    public class RoleToBorderBrushConverter : IValueConverter
    {
        private static readonly Brush UserBorder = new SolidColorBrush(Color.FromRgb(0xB7, 0xDD, 0xDB));
        private static readonly Brush OtherBorder = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE5));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Role role && role == Role.User ? UserBorder : OtherBorder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Maps a boolean to <see cref="Visibility.Visible"/> / <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}
