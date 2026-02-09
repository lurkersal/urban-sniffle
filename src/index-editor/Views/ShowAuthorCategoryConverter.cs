using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    // Returns true when the author field should be shown for the given category.
    public class ShowAuthorCategoryConverter : IValueConverter
    {
        private static readonly string[] CategoriesShowingAuthor = new[]
        {
            "humour",
            "feature",
            "fiction",
            "editorial",
            "interview"
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var cat = (value as string)?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(cat)) return false;
            foreach (var c in CategoriesShowingAuthor)
                if (cat.Equals(c, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

