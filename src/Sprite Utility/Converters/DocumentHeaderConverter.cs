using System;
using System.Globalization;
using System.Windows.Data;

namespace Boxer.Converters
{
    public class DocumentHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var header = "";
            if (!(values[0] is string)) return header;
            var name = values[0] as string;
            var foldersCount = (int)values[1];
            header = name + Environment.NewLine + "Folders: " + foldersCount;
            return header;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}