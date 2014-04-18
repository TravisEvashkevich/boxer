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
        public SearchFilterView()
        {
            InitializeComponent();
        }

        private void SearchBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var instance = ServiceLocator.Current.GetInstance<SearchFilterVM>();
                if (SearchBox.Text == "")
                {
                    instance.ResetDocument();
                }
                if (SearchBox.Text != instance.SearchText)
                {
                    instance.SearchText = SearchBox.Text;
                    instance.ExecuteSearchCommand(instance);
                }
            }
        }

        private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
