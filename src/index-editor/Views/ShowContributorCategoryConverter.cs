using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    // Returns true when the contributor (field 6) should be shown for the category.
    public class ShowContributorCategoryConverter : IValueConverter
    {
        private static readonly string[] ContributorCategories = new[]
        {
            // categories that use a contributor field (either author or photographer)
            "humour", "feature", "fiction", "editorial", "interview", "model", "cover", "photographer", "cartoons", "illustration", "review", "motoring"
        };

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var cat = (value as string)?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(cat)) return false;
            foreach (var c in ContributorCategories)
                if (cat.Equals(c, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

