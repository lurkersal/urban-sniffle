using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IndexEditor.Views
{
    /// <summary>
    /// Converts Segment.HasMissingPages to a foreground brush.
    /// Returns Red if pages are missing, otherwise Black.
    /// </summary>
    public class SegmentMissingPagesForegroundConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool hasMissing && hasMissing)
            {
                return new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

