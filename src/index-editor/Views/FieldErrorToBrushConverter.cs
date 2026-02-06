using System;
using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia.Media;

namespace IndexEditor.Views
{
    public class FieldErrorToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool hasError = false;
            if (value is bool b) hasError = b;
            if (hasError)
                return new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0xCB)); // light pink
            return new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)) { Opacity = 0.0 }; // transparent
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
