using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IndexEditor.Views
{
    public class CategoryToHexConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                var cat = (value as string) ?? string.Empty;
                // Leverage existing ArticleCategoryToColorConverter indirectly by recreating logic here
                // to produce a hex string matching the brush colors used elsewhere.
                Color c;
                switch (cat?.ToLowerInvariant())
                {
                    case "group": c = Color.FromRgb(0x8E, 0x24, 0xAA); break;
                    case "cover": c = Color.FromRgb(0xFF, 0xE0, 0xB2); break;
                    case "index": c = Color.FromRgb(0x90, 0xCA, 0xF9); break;
                    case "editorial": c = Color.FromRgb(0xA5, 0xD6, 0xA7); break;
                    case "cartoons": c = Color.FromRgb(0xFF, 0xCC, 0x80); break;
                    case "letters": c = Color.FromRgb(0xD1, 0xC4, 0xE9); break;
                    case "wives": c = Color.FromRgb(0xF8, 0xBB, 0xD0); break;
                    case "model": c = Color.FromRgb(0xB2, 0xDF, 0xDB); break;
                    case "pinup": c = Color.FromRgb(0xFF, 0xAB, 0x91); break;
                    case "fiction": c = Color.FromRgb(0xCE, 0x93, 0xD8); break;
                    case "feature": c = Color.FromRgb(0xFF, 0xF9, 0xC4); break;
                    case "humour": c = Color.FromRgb(0x80, 0xDE, 0xEA); break;
                    case "motoring": c = Color.FromRgb(0xB0, 0xBE, 0xC5); break;
                    case "travel": c = Color.FromRgb(0xC5, 0xE1, 0xA5); break;
                    case "review": c = Color.FromRgb(0xFF, 0xF1, 0xB6); break;
                    case "illustrations": c = Color.FromRgb(0xA7, 0xFF, 0xEB); break;
                    case "interview": c = Color.FromRgb(0xFF, 0xD7, 0xB2); break;
                    case "contents":
                    case "content": c = Color.FromRgb(0xFF, 0xF9, 0xC4); break;
                    default: c = Color.FromRgb(0xE0, 0xE0, 0xE0); break;
                }
                return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            catch
            {
                return "#E0E0E0";
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
