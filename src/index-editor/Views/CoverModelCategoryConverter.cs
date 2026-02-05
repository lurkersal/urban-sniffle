using System;
using Avalonia.Data.Converters;
using System.Globalization;

namespace IndexEditor.Views
{
    public class CoverModelCategoryConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                return category.Equals("Cover", StringComparison.OrdinalIgnoreCase)
                    || category.Equals("Model", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
