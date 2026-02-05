using System;
using Avalonia.Data.Converters;
using System.Globalization;

namespace IndexEditor.Views
{
    public class ArticleCategoryDisplayConverter : IValueConverter
    {
        // Returns: 0 = Category only, 1 = Category+Title, 2 = Cover/Model fields, 3 = Category+Title+Photographer only
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                if (category.Equals("Contents", StringComparison.OrdinalIgnoreCase) || category.Equals("Content", StringComparison.OrdinalIgnoreCase))
                    return 0;
                if (category.Equals("Letters", StringComparison.OrdinalIgnoreCase))
                    return 1;
                if (category.Equals("Cover", StringComparison.OrdinalIgnoreCase) || category.Equals("Model", StringComparison.OrdinalIgnoreCase))
                    return 2;
                if (category.Equals("Review", StringComparison.OrdinalIgnoreCase) ||
                    category.Equals("Fiction", StringComparison.OrdinalIgnoreCase) ||
                    category.Equals("Motoring", StringComparison.OrdinalIgnoreCase))
                    return 3;
                return 1; // Default: Category+Title
            }
            return 1;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
