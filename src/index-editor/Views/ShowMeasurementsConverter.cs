using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Common.Shared;

namespace IndexEditor.Views
{
    // Shows the Measurements editor if the article category is Model/Cover OR if the article already has measurements populated.
    public class ShowMeasurementsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ArticleLine article)
            {
                var cat = (article.Category ?? string.Empty).Trim().ToLowerInvariant();
                if (cat == "model" || cat == "cover")
                    return true;
                if (article.Measurements != null && article.Measurements.Count > 0 && !string.IsNullOrWhiteSpace(article.Measurements[0]))
                    return true;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

