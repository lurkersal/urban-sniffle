using System;
using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia.Media;

namespace IndexEditor.Views
{
    public class ArticleCategoryToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                switch (category.ToLowerInvariant())
                {
                    case "group": return new SolidColorBrush(Color.FromRgb(0x8E, 0x24, 0xAA)); // Purple
                    case "cover": return new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0xB2)); // Light orange
                    case "index": return new SolidColorBrush(Color.FromRgb(0x90, 0xCA, 0xF9)); // Light blue
                    case "editorial": return new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7)); // Light green
                    case "cartoons": return new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x80)); // Light amber
                    case "letters": return new SolidColorBrush(Color.FromRgb(0xD1, 0xC4, 0xE9)); // Light purple
                    case "wives": return new SolidColorBrush(Color.FromRgb(0xF8, 0xBB, 0xD0)); // Pink
                    case "model": return new SolidColorBrush(Color.FromRgb(0xB2, 0xDF, 0xDB)); // Light teal
                    case "pinup": return new SolidColorBrush(Color.FromRgb(0xFF, 0xAB, 0x91)); // Light red
                    case "fiction": return new SolidColorBrush(Color.FromRgb(0xCE, 0x93, 0xD8)); // Lavender
                    case "feature": return new SolidColorBrush(Color.FromRgb(0xFF, 0xF9, 0xC4)); // Light yellow
                    case "humour": return new SolidColorBrush(Color.FromRgb(0x80, 0xDE, 0xEA)); // Cyan
                    case "motoring": return new SolidColorBrush(Color.FromRgb(0xB0, 0xBE, 0xC5)); // Gray blue
                    case "travel": return new SolidColorBrush(Color.FromRgb(0xC5, 0xE1, 0xA5)); // Light lime
                    case "review": return new SolidColorBrush(Color.FromRgb(0xFF, 0xF1, 0xB6)); // Light gold
                    case "illustrations": return new SolidColorBrush(Color.FromRgb(0xA7, 0xFF, 0xEB)); // Mint
                    case "interview": return new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0xB2)); // Peach
                    case "contents":
                    case "content": return new SolidColorBrush(Color.FromRgb(0xFF, 0xF9, 0xC4)); // Light yellow
                    default: return new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)); // Default gray
                }
            }
            return new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
