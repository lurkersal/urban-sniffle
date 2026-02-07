using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IndexEditor.Views
{
    public class CategoryToSubtleBackgroundConverter : IValueConverter
    {
        private static Color ColorForCategory(string category)
        {
            switch (category?.ToLowerInvariant())
            {
                case "group": return Color.FromRgb(0x8E, 0x24, 0xAA);
                case "cover": return Color.FromRgb(0xFF, 0xE0, 0xB2);
                case "index": return Color.FromRgb(0x90, 0xCA, 0xF9);
                case "editorial": return Color.FromRgb(0xA5, 0xD6, 0xA7);
                case "cartoons": return Color.FromRgb(0xFF, 0xCC, 0x80);
                case "letters": return Color.FromRgb(0xD1, 0xC4, 0xE9);
                case "wives": return Color.FromRgb(0xF8, 0xBB, 0xD0);
                case "model": return Color.FromRgb(0xB2, 0xDF, 0xDB);
                case "pinup": return Color.FromRgb(0xFF, 0xAB, 0x91);
                case "fiction": return Color.FromRgb(0xCE, 0x93, 0xD8);
                case "feature": return Color.FromRgb(0xFF, 0xF9, 0xC4);
                case "humour": return Color.FromRgb(0x80, 0xDE, 0xEA);
                case "motoring": return Color.FromRgb(0xB0, 0xBE, 0xC5);
                case "travel": return Color.FromRgb(0xC5, 0xE1, 0xA5);
                case "review": return Color.FromRgb(0xFF, 0xF1, 0xB6);
                case "illustrations": return Color.FromRgb(0xA7, 0xFF, 0xEB);
                case "interview": return Color.FromRgb(0xFF, 0xD7, 0xB2);
                case "contents":
                case "content": return Color.FromRgb(0xFF, 0xF9, 0xC4);
                default: return Color.FromRgb(0xE0, 0xE0, 0xE0);
            }
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                var cat = value as string ?? string.Empty;
                var c = ColorForCategory(cat);
                double lum = (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
                // If category is light, use a faint black overlay; otherwise a faint white overlay
                byte alpha = 0x33; // ~20% opacity (stronger so segment text is readable)
                if (lum > 0.55)
                    return new SolidColorBrush(Color.FromArgb(alpha, 0x00, 0x00, 0x00));
                else
                    return new SolidColorBrush(Color.FromArgb(alpha, 0xFF, 0xFF, 0xFF));
            }
            catch
            {
                return new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0x00, 0x00));
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
