using System;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace IndexEditor.Views
{
    public class SelectedArticleBorderConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
                return new SolidColorBrush(Colors.Red);
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
