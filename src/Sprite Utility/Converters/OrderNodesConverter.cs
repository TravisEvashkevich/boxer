using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using Boxer.Core;
using Boxer.Data;

namespace Boxer.Converters
{
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
}