using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Boxer.Data;
using Boxer.ViewModel;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.Views
{
    /// <summary>
    /// Interaction logic for SearchFilterView.xaml
    /// </summary>
    public partial class SearchFilterView : UserControl
    {
        private SearchFilterVM _searchFilterVm = ServiceLocator.Current.GetInstance<SearchFilterVM>();
        public SearchFilterView()
        {
            InitializeComponent();
        }

        private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Text != _searchFilterVm.SearchText)
            {
                _searchFilterVm.SearchText = SearchBox.Text;
                _searchFilterVm.ExecuteSearchCommand(_searchFilterVm);
            }
        }
    }
}
