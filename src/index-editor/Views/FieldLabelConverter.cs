using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    public class FieldLabelConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var field = (parameter as string)?.ToLowerInvariant() ?? string.Empty;
            var category = (value as string)?.ToLowerInvariant() ?? string.Empty;

            switch (field)
            {
                case "photographer":
                    if (category == "cartoons") return "Cartoonist:";
                    if (category == "motoring" || category == "feature" || category == "fiction" || category == "review") return "Author:";
                    return "Photographer:";
                case "model":
                    return "Model:";
                case "age":
                    return "Age:";
                case "title":
                    return "Title:";
                default:
                    return parameter ?? string.Empty;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
