using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    public class NotNullToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNotNull = value != null;
            if (parameter is string p && p.Equals("Invert", StringComparison.OrdinalIgnoreCase))
                return !isNotNull;
            return isNotNull;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
