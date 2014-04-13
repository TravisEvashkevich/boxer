using System;
using System.Globalization;
using System.Windows.Data;

namespace Boxer.Converters
{
    public class IsOpenToHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value is bool)
            {
                var result = (bool) value;
                if (result)
                {
                    return "Mark as closed";
                }
                else
                {
                    return "Mark as open";
                }
            }

            return "FATAL ERROR ;c";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }
}