using System;
using Avalonia.Data.Converters;
using Common.Shared;
using System.Globalization;

namespace IndexEditor.Views
{
    public class ActiveSegmentToTextConverter : IValueConverter
    {
        // Converts Segment -> display text. If segment is active (End==null) show "start → currentPage",
        // otherwise show segment.Display (e.g., "13" or "110-114").
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                var seg = value as Segment;
                if (seg == null) return "— none —";
                if (seg.IsActive)
                {
                    var current = IndexEditor.Shared.EditorState.CurrentPage;
                    return $"{seg.Start} → {current}";
                }
                return seg.Display ?? "—";
            }
            catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("ActiveSegmentToTextConverter.Convert", ex); return "— none —"; }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
