using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    // Returns true when the age field should be shown for the category.
    // Model, Cover, and Group categories show age, but Wives does not.
    public class ShowAgeCategoryConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var cat = (value as string)?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(cat)) return false;
            
            // Only Model, Cover, and Group show age field
            return cat == "model" || cat == "cover" || cat == "group";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

