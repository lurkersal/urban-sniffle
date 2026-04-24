using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Common.Shared;

namespace IndexEditor.Views
{
    // Shows the Measurements editor if the article category is Model/Cover/Group.
    public class ShowMeasurementsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Can receive either an ArticleLine object or a Category string
            string? cat = null;
            
            if (value is ArticleLine article)
            {
                cat = (article.Category ?? string.Empty).Trim().ToLowerInvariant();
            }
            else if (value is string categoryString)
            {
                cat = categoryString.Trim().ToLowerInvariant();
            }
            
            if (!string.IsNullOrEmpty(cat))
            {
                if (cat == "model" || cat == "cover" || cat == "group")
                    return true;
            }
            
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

