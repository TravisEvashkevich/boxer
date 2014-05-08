using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Boxer.Core;
using Boxer.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;

namespace Boxer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //want to save in a var so I can use across methods, can't load right at the start cause it doesn't exist till after the initcomp
        private MainWindowVM _mainWindowVm = null;
        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;

            Messenger.Default.Register<CloseMainWindowMessage>(this, p => Close());

            Icon = new BitmapImage(new Uri("icon@2x.png", UriKind.Relative));

            TreeView.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));

            /*------------------------HotKeys----------------*/
            //Get the MainWindowViewModel as it has all the menu related commands
            _mainWindowVm = ServiceLocator.Current.GetInstance<MainWindowVM>();
            InputBindings.Add(new KeyBinding(_mainWindowVm.OpenDocumentCommand, new KeyGesture(Key.O, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.NewDocumentCommand, new KeyGesture(Key.N, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.SaveDocumentCommand, new KeyGesture(Key.S, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.SaveAsCommand, new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.CloseCommand, new KeyGesture(Key.Q, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.ExportCommand, new KeyGesture(Key.E, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.RemoveCommand, new KeyGesture(Key.Delete)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.CopyCommand, new KeyGesture(Key.C, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.PasteCommand, new KeyGesture(Key.V, ModifierKeys.Control)));

            //Reimport commands
            InputBindings.Add(new KeyBinding(_mainWindowVm.ReimportFromNewPathCommand, new KeyGesture(Key.R, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(_mainWindowVm.ReimportMultipleCommand, new KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Shift))); 

        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!Glue.Instance.DocumentIsSaved)
            {
                if (MessageBox.Show("You have Un-Saved Changes. Would you like to save them now?", "Un-Saved Changes",
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    //If they want to save then we save else whatever.
                    Glue.Instance.Document.Save(false);
                }
            }
            
        }

        private void TreeView_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl)))
            {
                _mainWindowVm.JumpBackOneImageFrame();
            }
            else if (e.Key == Key.Enter)
            {
                _mainWindowVm.JumpToNextImageFrame();

            }
            
        }
    }
}
