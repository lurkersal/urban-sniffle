using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IndexEditor.Views
{
    public class CategoryToContrastBrushConverter : IValueConverter
    {
        // Duplicate the color mapping from ArticleCategoryToColorConverter to determine luminance
        private static Color ColorForCategory(string category)
        {
            switch (category?.ToLowerInvariant())
            {
                case "group": return Color.FromRgb(0x8E, 0x24, 0xAA); // Purple
                case "cover": return Color.FromRgb(0xFF, 0xE0, 0xB2); // Light orange
                case "index": return Color.FromRgb(0x90, 0xCA, 0xF9); // Light blue
                case "editorial": return Color.FromRgb(0xA5, 0xD6, 0xA7); // Light green
                case "cartoons": return Color.FromRgb(0xFF, 0xCC, 0x80); // Light amber
                case "letters": return Color.FromRgb(0xD1, 0xC4, 0xE9); // Light purple
                case "wives": return Color.FromRgb(0xF8, 0xBB, 0xD0); // Pink
                case "model": return Color.FromRgb(0xB2, 0xDF, 0xDB); // Light teal
                case "pinup": return Color.FromRgb(0xFF, 0xAB, 0x91); // Light red
                case "fiction": return Color.FromRgb(0xCE, 0x93, 0xD8); // Lavender
                case "feature": return Color.FromRgb(0xFF, 0xF9, 0xC4); // Light yellow
                case "humour": return Color.FromRgb(0x80, 0xDE, 0xEA); // Cyan
                case "motoring": return Color.FromRgb(0xB0, 0xBE, 0xC5); // Gray blue
                case "travel": return Color.FromRgb(0xC5, 0xE1, 0xA5); // Light lime
                case "review": return Color.FromRgb(0xFF, 0xF1, 0xB6); // Light gold
                case "illustrations": return Color.FromRgb(0xA7, 0xFF, 0xEB); // Mint
                case "interview": return Color.FromRgb(0xFF, 0xD7, 0xB2); // Peach
                case "contents":
                case "content": return Color.FromRgb(0xFF, 0xF9, 0xC4); // Light yellow
                default: return Color.FromRgb(0xE0, 0xE0, 0xE0); // Default gray
            }
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                var cat = value as string ?? string.Empty;
                var c = ColorForCategory(cat);
                // Calculate luminance; using standard Rec.709 coefficients
                double lum = (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
                // If luminance is low (dark color) choose white text; otherwise black
                return lum < 0.55 ? new SolidColorBrush(Color.FromRgb(0xFF,0xFF,0xFF)) : new SolidColorBrush(Color.FromRgb(0x22,0x22,0x22));
            }
            catch
            {
                return new SolidColorBrush(Color.FromRgb(0x22,0x22,0x22));
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
