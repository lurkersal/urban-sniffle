using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    // Returns true when the photographer field should be shown for the given category.
    public class ShowPhotographerCategoryConverter : IValueConverter
    {
        private static readonly string[] CategoriesShowingPhotographer = new[]
        {
            "cartoons",
            "model",
            "cover",
            "photographer",
            "feature",
            "fiction",
            "review",
            "motoring"
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var cat = (value as string)?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(cat)) return false;
            foreach (var c in CategoriesShowingPhotographer)
                if (cat.Equals(c, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
