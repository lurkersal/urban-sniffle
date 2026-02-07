using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace IndexEditor.Views
{
    // Returns a darker accent SolidColorBrush for a category color to ensure visible contrast against light backgrounds.
    public class CategoryToAccentBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                var cat = (value as string) ?? string.Empty;
                Color baseColor;
                switch (cat?.ToLowerInvariant())
                {
                    case "group": baseColor = Color.FromRgb(0x8E, 0x24, 0xAA); break;
                    case "cover": baseColor = Color.FromRgb(0xFF, 0xE0, 0xB2); break;
                    case "index": baseColor = Color.FromRgb(0x90, 0xCA, 0xF9); break;
                    case "editorial": baseColor = Color.FromRgb(0xA5, 0xD6, 0xA7); break;
                    case "cartoons": baseColor = Color.FromRgb(0xFF, 0xCC, 0x80); break;
                    case "letters": baseColor = Color.FromRgb(0xD1, 0xC4, 0xE9); break;
                    case "wives": baseColor = Color.FromRgb(0xF8, 0xBB, 0xD0); break;
                    case "model": baseColor = Color.FromRgb(0xB2, 0xDF, 0xDB); break;
                    case "pinup": baseColor = Color.FromRgb(0xFF, 0xAB, 0x91); break;
                    case "fiction": baseColor = Color.FromRgb(0xCE, 0x93, 0xD8); break;
                    case "feature": baseColor = Color.FromRgb(0xFF, 0xF9, 0xC4); break;
                    case "humour": baseColor = Color.FromRgb(0x80, 0xDE, 0xEA); break;
                    case "motoring": baseColor = Color.FromRgb(0xB0, 0xBE, 0xC5); break;
                    case "travel": baseColor = Color.FromRgb(0xC5, 0xE1, 0xA5); break;
                    case "review": baseColor = Color.FromRgb(0xFF, 0xF1, 0xB6); break;
                    case "illustrations": baseColor = Color.FromRgb(0xA7, 0xFF, 0xEB); break;
                    case "interview": baseColor = Color.FromRgb(0xFF, 0xD7, 0xB2); break;
                    case "contents":
                    case "content": baseColor = Color.FromRgb(0xFF, 0xF9, 0xC4); break;
                    default: baseColor = Color.FromRgb(0xE0, 0xE0, 0xE0); break;
                }

                // Darken the color to make it an accent: multiply RGB by factor
                double factor = 0.5; // makes it noticeably darker
                byte r = (byte)Math.Max(0, Math.Min(255, (int)(baseColor.R * factor)));
                byte g = (byte)Math.Max(0, Math.Min(255, (int)(baseColor.G * factor)));
                byte b = (byte)Math.Max(0, Math.Min(255, (int)(baseColor.B * factor)));

                return new SolidColorBrush(Color.FromRgb(r, g, b));
            }
            catch
            {
                return new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
