using System;
using System.Globalization;
using System.Windows.Data;
using Boxer.Core;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.Converters
{
    public class IsDocumentSavedToHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value is bool)
            {
                var glue = ServiceLocator.Current.GetInstance<Glue>();
                if (glue.Document != null)
                {
                    if (glue.DocumentIsSaved)
                    {
                        return "Sprite Utility [" + glue.Document.Filename + "]";
                    }
                    else
                    {
                        return "Sprite Utility [" + glue.Document.Filename + "*]";
                    }
                }
                else
                {
                    return "Sprite Utility";
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