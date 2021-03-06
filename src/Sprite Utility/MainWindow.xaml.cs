﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Boxer.Converters;
using Boxer.Core;
using Boxer.Data;
using Boxer.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using Keyboard = System.Windows.Input.Keyboard;
using Point = System.Windows.Point;

namespace Boxer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //want to save in a var so I can use across methods, can't load right at the start cause it doesn't exist till after the initcomp
        private MainWindowVM _mainWindowVm = null;
        private bool _isDragging;
        private Point _startPoint;
        private bool _isReordering;

        public TreeViewItem ReOrderItem { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

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
            //Merge Command
            InputBindings.Add(new KeyBinding(_mainWindowVm.MergeCommand, new KeyGesture(Key.M, ModifierKeys.Control)));

        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (!Glue.Instance.DocumentIsSaved)
            {
               var result =  MessageBox.Show("You have Un-Saved Changes. Would you like to save them now?", "Un-Saved Changes",
                    MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    //If they want to save then we save else whatever.
                    Glue.Instance.Document.Save(false);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
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

        private void TreeView_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void TreeView_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    ReOrderDrag(e);
                }
            }
        }

        private void ReOrderDrag(MouseEventArgs e)
        {
            _isDragging = true;
            _isReordering = true;
            //We can get the item that you're "dragging" and set it to the currentSelectedAction already
            var pos = MouseUtilities.GetMousePosition(TreeView);
            var element = GetItemAtLocation<TreeViewItem>(pos);
            if (element == null || (element.Header.GetType() != typeof(Folder) && element.Header.GetType() != typeof(ImageData)))
            {
                _isDragging = false;
                _isReordering = false;
                return;
            }
            ReOrderItem = element;
            DataObject data = new DataObject(DataFormats.Text, "abcd");
            DragDropEffects de = DragDrop.DoDragDrop(TreeView, data, DragDropEffects.Move);

            _isDragging = false;
            _isReordering = false;
        }

        private void TreeView_OnDrop(object sender, DragEventArgs e)
        {
            #region Folder Drops
            //Get the currentSelected from the MainWindowVM and see if it's a Folder/ImageData
            if (ReOrderItem.Header is Folder)
            {
                //find drop spot (get item you're dropping on, null = not on an item, 
                var pos = MouseUtilities.GetMousePosition(TreeView);
                var dropItem = GetItemAtLocation<TreeViewItem>(pos);
                if(dropItem == null)
                    return;
                
                if ( dropItem.Header is Folder)
                {
                   if ((ReOrderItem.Header as Folder).Name != (dropItem.Header as Folder).Name && !FolderIsChildOf(ReOrderItem.Header as Folder, dropItem.Header as Folder))
                    {
                        var targetFolder = dropItem.Header as Folder;
                        var sourceFolder = ReOrderItem.Header as Folder;

                        targetFolder.Children.Add(sourceFolder);
                        sourceFolder.Parent.Children.Remove(sourceFolder);
                        sourceFolder.Parent = targetFolder;
                        Glue.Instance.DocumentIsSaved = false;
                    }
                }
                else if (dropItem.Header is Document)
                {
                    var targetFolder = dropItem.Header as Document;
                    var sourceFolder = ReOrderItem.Header as Folder;

                    targetFolder.Children.Add(sourceFolder);
                    sourceFolder.Parent.Children.Remove(sourceFolder);
                    sourceFolder.Parent = targetFolder;
                    Glue.Instance.DocumentIsSaved = false;
                }
            }
            #endregion
            #region Image Drops
            else if (ReOrderItem.Header is ImageData)
            {
                //find drop spot (get item you're dropping on, null = not on an item, 
                var pos = MouseUtilities.GetMousePosition(TreeView);
                var dropItem = GetItemAtLocation<TreeViewItem>(pos);
                if (dropItem == null)
                    return;
                if (dropItem.Header is Folder)
                {
                    var targetFolder = dropItem.Header as Folder;
                    var sourceImage = ReOrderItem.Header as ImageData;

                    targetFolder.Children.Add(sourceImage);
                    sourceImage.Parent.Children.Remove(sourceImage);
                    sourceImage.Parent = targetFolder;
                    Glue.Instance.DocumentIsSaved = false;
                }
                /*//if we dropped on another image I guess we just add to the folder that image is contained in
                 * //UnComment if you want to have drop on Images to add to folders (seemed weird when I coded it and seems weird when I try to use the program)
                else if (dropItem.Header is ImageData)
                {
                    var targetFolder = dropItem.Header as ImageData;
                    var sourceImage = ReOrderItem.Header as ImageData;

                    targetFolder.Parent.Children.Add(sourceImage);
                    sourceImage.Parent.Children.Remove(sourceImage);
                    sourceImage.Parent = targetFolder.Parent;
                    Glue.Instance.DocumentIsSaved = false;
                }*/



            }
            #endregion
        }

        private bool FolderIsChildOf(Folder parentFolder, Folder childFolder)
        {
            foreach (var child in parentFolder.Children)
            {
                if (child is Folder && child.Name == childFolder.Name)
                {
                    return true;
                }
            }
            return false;
        }

        #region Visual Finder Methods

        //Use these next two methods to find the object that is under the mouse cursor at drop AND find the parent (else it will give you a label or a tbox or something)
        T GetItemAtLocation<T>(Point location)
        {
            T foundItem = default(T);
            HitTestResult hitTestResults = VisualTreeHelper.HitTest(TreeView, location);


            if (hitTestResults != null)
            {
                var treeViewItem = GetParentOfType<TreeViewItem>(hitTestResults.VisualHit);

                object dataObject = treeViewItem;

                if (treeViewItem is T)
                {
                    foundItem = (T)dataObject;
                }
            }

            return foundItem;
        }

        private T GetParentOfType<T>(DependencyObject o) where T : DependencyObject
        {
            DependencyObject parent = o;
            do
            {
                parent = VisualTreeHelper.GetParent(parent);
                if (parent == null)
                    break;
                if (parent.GetType() == typeof(T))
                {
                    return (T)parent;
                }
            } while (parent != null);
            return null;
        }

        #endregion

    }
}
