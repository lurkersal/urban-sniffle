using System;
using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia.Media;
using Common.Shared;

namespace IndexEditor.Views
{
    public class ArticleActiveSegmentBrushConverter : IValueConverter
    {
        // Input: ArticleLine (the Content). Return a Brush: if article has active segment -> use category color (fallback #F0A) else light gray.
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                var art = value as ArticleLine;
                if (art == null) return new SolidColorBrush(Color.Parse("#E8E8E8"));
                var seg = art.ActiveSegment;
                if (seg != null && seg.IsActive)
                {
                    // reuse ArticleCategoryToColorConverter logic indirectly: attempt to map category string to color using that converter
                    try
                    {
                        var catConv = new ArticleCategoryToColorConverter();
                        var brush = catConv.Convert(art.Category, typeof(IBrush), null, culture) as IBrush;
                        if (brush != null) return brush;
                    }
                    catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("ArticleActiveSegmentBrushConverter: category color conversion", ex); }
                    // fallback color for active
                    return new SolidColorBrush(Color.Parse("#FF00A0"));
                }
                // inactive shade
                return new SolidColorBrush(Color.Parse("#E8E8E8"));
            }
            catch (Exception ex) { IndexEditor.Shared.DebugLogger.LogException("ArticleActiveSegmentBrushConverter: outer", ex); return new SolidColorBrush(Color.Parse("#E8E8E8")); }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
