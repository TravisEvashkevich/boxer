using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Boxer.Core;
using Boxer.Data;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.Converters
{
    public class ImageFrameHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            string header = "";

            if (values[0] is ImageFrame)
            {
                var node = values[0] as ImageFrame;
               // var childrens = values[1] as ObservableCollection<INode>;
                var index = node.Parent.Children.IndexOf(node);
                string name = "Frame " + (index + 1);
                int width = (int) values[2];
                int height = (int) values[3];
                string openCloseLabel = (bool) values[4] ? "Open" : "Close";

                int duration = (int) values[5];

                int polygonGroupsCount = (int) values[6];

                header = name + " (" + width + ", " + height + ", " + openCloseLabel + ") - " + duration + "ms" + Environment.NewLine
                    + "Polygon Groups: " + polygonGroupsCount;
            }

            return header;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DocumentHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            string header = "";

            if (values[0] is string)
            {
                string name = values[0] as string;

                int foldersCount = (int)values[1];

                header = name + Environment.NewLine
                    + "Folders: " + foldersCount;
            }

            return header;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CollapseIfTrueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            return (bool?)value == true ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

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

    public class ShowIfTrueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            return (bool?)value == true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class OrderNodesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value is ObservableCollection<INode>)
            {
                var collection = value as ObservableCollection<INode>;

                AutoRefreshCollectionViewSource view = new AutoRefreshCollectionViewSource();
                view.Source = collection;
                var sort = new SortDescription("Type", ListSortDirection.Ascending);
                view.SortDescriptions.Add(sort);
                sort = new SortDescription("Name", ListSortDirection.Ascending);
                view.SortDescriptions.Add(sort);

                return view.View;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorToBrushConterver : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value is Color)
            {
                return new SolidColorBrush((Color)value);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }
}
