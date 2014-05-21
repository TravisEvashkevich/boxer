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
using System.Windows.Shapes;
using Boxer.ViewModel;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.Views
{
    /// <summary>
    /// Interaction logic for MergeWindow.xaml
    /// </summary>
    public partial class MergeWindow : Window
    {
        private MergeVM _mergeVm = ServiceLocator.Current.GetInstance<MergeVM>();
        public MergeWindow()
        {
            InitializeComponent();

            InputBindings.Add(new KeyBinding(_mergeVm.SelectAllCommand, new KeyGesture(Key.A, ModifierKeys.Control)));
        }
    }
}
