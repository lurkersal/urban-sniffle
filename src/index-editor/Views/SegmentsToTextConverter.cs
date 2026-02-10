using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    public class SegmentsToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IEnumerable<Common.Shared.Segment> segs)
            {
                try
                {
                    var parts = segs.Select(s => s.Display).ToList();
                    return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
                }
                catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("SegmentsToTextConverter.Convert", ex); }
            }
            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
