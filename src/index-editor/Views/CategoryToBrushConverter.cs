using System;
using System.Collections.Generic;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IndexEditor.Views
{
    public class CategoryToBrushConverter : IValueConverter
    {
        private static readonly Dictionary<string, Color> CategoryColors = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Cover", Colors.LightSkyBlue },
            { "Index", Colors.LightGray },
            { "Editorial", Colors.LightGoldenrodYellow },
            { "Cartoons", Colors.PaleGoldenrod },
            { "Letters", Colors.LightGreen },
            { "Model", Colors.MistyRose },
            { "Fiction", Colors.LavenderBlush },
            { "Feature", Colors.LightSalmon },
            { "Review", Colors.LightCyan },
            { "Motoring", Colors.LightSteelBlue },
            { "Humour", Colors.LemonChiffon }
        };

        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                if (CategoryColors.TryGetValue(s.Trim(), out var c))
                    return new SolidColorBrush(c);
                var hash = Math.Abs(s.Trim().GetHashCode());
                byte r = (byte)(200 + (hash % 56));
                byte g = (byte)(180 + ((hash / 56) % 56));
                byte b = (byte)(160 + ((hash / (56 * 56)) % 56));
                return new SolidColorBrush(Color.FromRgb(r, g, b));
            }
            return new SolidColorBrush(Colors.WhiteSmoke);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
