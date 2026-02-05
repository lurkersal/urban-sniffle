using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace IndexEditor.Views
{
    public class ArticleFieldFriendlyTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is IList<string> strList)
                return string.Join(", ", strList);


            if (value is IList<int?> intList)
                return string.Join(", ", intList.Where(x => x.HasValue).Select(x => x.Value));

            if (value is IList<int> intList2)
                return string.Join(", ", intList2);

            if (value is int i)
                return i.ToString();

            if (value is int?)
            {
                var ni = (int?)value;
                if (ni.HasValue)
                    return ni.Value.ToString();
                else
                    return string.Empty;
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
