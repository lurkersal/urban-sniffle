using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    // Converter: takes category string as value and a parameter like "1" or "1,3" and returns true if
    // ArticleCategoryDisplayConverter returns any of those numeric modes.
    public class CategoryDisplayModeMatchesConverter : IValueConverter
    {
        private readonly ArticleCategoryDisplayConverter _modeConverter = new ArticleCategoryDisplayConverter();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var param = parameter as string ?? string.Empty;
            var parts = param.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var wanted = new System.Collections.Generic.HashSet<int>();
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out var n)) wanted.Add(n);
            }

            var modeObj = _modeConverter.Convert(value, typeof(int), null, culture);
            if (modeObj is int mode)
            {
                return wanted.Count == 0 ? false : wanted.Contains(mode);
            }
            // fallback: false
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
