using System;
using Avalonia.Data.Converters;
using System.Globalization;

namespace IndexEditor.Views
{
    /// <summary>
    /// Converts HasThumbnail boolean to row height.
    /// Articles with thumbnails get double the height (200px), others get standard height (100px).
    /// </summary>
    public class ArticleRowHeightConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool hasThumbnail && hasThumbnail)
            {
                return 200.0; // Double height for articles with thumbnails
            }
            return 100.0; // Standard height for articles without thumbnails
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

