using System;
using System.Globalization;
using System.Windows.Data;
using Boxer.Data;

namespace Boxer.Converters
{
    public class ImageFrameHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
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
}