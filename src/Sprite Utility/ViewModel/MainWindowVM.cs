using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Boxer.Core;
using Boxer.Data;
using Boxer.Properties;
using Boxer.Services;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using SpriteUtility.Data.Export;
using MessageBox = System.Windows.MessageBox;

namespace Boxer.ViewModel
{

    public class CloseMainWindowMessage
    {
        
    }
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainWindowVM : MainViewModel
    {
        private string _lastPolygonGroupName;
        private NodeWithName _currentSelectedNode;
        private Polygon _copyPolygon;

        public Glue Glue
        {
            get { return ServiceLocator.Current.GetInstance<Glue>(); }
        }

        private readonly ViewModelLocator _viewModelLocator;

        private MainViewModel _currentView;

        public MainViewModel CurrentView
        {
            get { return _currentView; }
            set { Set(ref _currentView, value); }
        }

        private ImageFrame _imageFrame;

        public ImageFrame ImageFrame
        {
            get { return _imageFrame; }
            set { Set(ref _imageFrame, value); }
        }

        private ObservableCollection<Document> _documents;

        public ObservableCollection<Document> Documents
        {
            get { return _documents; }
            set { Set(ref _documents, value); }
        }

        private bool _isDocumentViewVisible;

        public bool IsDocumentViewVisible
        {
            get { return _isDocumentViewVisible; }
            set { Set(ref _isDocumentViewVisible, value); }
        }

        private bool _isImageViewerViewVisible;

        public bool IsImageViewerViewVisible
        {
            get { return _isImageViewerViewVisible; }
            set { Set(ref _isImageViewerViewVisible, value); }
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainWindowVM()
        {
            Documents = new ObservableCollection<Document>();

            IsDocumentViewVisible = true;

            _viewModelLocator = new ViewModelLocator();

            Settings.Default.PropertyChanged += Default_PropertyChanged;

            TraceService.SetDisplayUnitToSimUnitRatio(Settings.Default.SimulationRatio);

            if (IsInDesignMode)
            {
                CurrentView = _viewModelLocator.DocumentView;
            }
        }

        static void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SimulationRatio")
            {
                TraceService.SetDisplayUnitToSimUnitRatio(Settings.Default.SimulationRatio);
            }
        }

        public void CreateNewPolygon()
        {
            if (_currentSelectedNode is Polygon)
            {
                var group = _currentSelectedNode.Parent as PolygonGroup;
                var newPoly = new Polygon();
                newPoly.Parent = group;
                newPoly.Name = "Polygon " + (group.Children.Count + 1);
                newPoly.Initialize();

                group.Children.Add(newPoly);

                var index = group.Children.IndexOf(newPoly);
                (group.Children[index] as Polygon).IsSelected = true;
                _currentSelectedNode = group.Children[index] as NodeWithName;
            }
        }

        //when we hit Ctrl+Enter in the mainwindow and we are on a poly or group we want to go up one frame and open the same p group
        public void JumpBackOneImageFrame()
        {
            //if we have an polygroup selected go up to the parent imagedata and go to the next child frame and then open to the polygroup.
            if (_currentSelectedNode is PolygonGroup)
            {
                //Close all the previous children
                var parentFrame = ((_currentSelectedNode as PolygonGroup).Parent as ImageFrame);
                if (parentFrame != null)
                {
                    parentFrame.Expanded = false;
                    foreach (var child in parentFrame.Children)
                    {
                        (child as NodeWithName).Expanded = false;
                        (child as NodeWithName).IsSelected = false;
                    }
                }

                //Get the index of the current Frame from the ImageData and close and deselect the imageFrame
                var index = (parentFrame.Parent as ImageData).Children.IndexOf(parentFrame);
                ((parentFrame.Parent as ImageData).Children[index] as
                    ImageFrame).IsSelected = false;
                ((parentFrame.Parent as ImageData).Children[index] as
                    ImageFrame).Expanded = false;

                //Check to see if there is another frame or not
                if (index - 1 >= 0)
                {
                    --index;
                }

                //Set the viewmodellocator varialbes and Open the next Frame
                _viewModelLocator.ImageFrameView.Frame = ((parentFrame.Parent as ImageData).Children[index] as ImageFrame);
                _viewModelLocator.ImageFrameView.Frame.Expanded = true;
                _viewModelLocator.ImageFrameView.Polygon = null;
                _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = true;
                _viewModelLocator.ImageFrameView.ShowPolygonTextBox = false;
                CurrentView = _viewModelLocator.ImageFrameView;
                var pGroup = _viewModelLocator.ImageFrameView.Frame.Children.First(t => t.Name == _lastPolygonGroupName);

                (pGroup as PolygonGroup).Expanded = true;

                _viewModelLocator.ImageFrameView.PolygonGroup = pGroup as PolygonGroup;
                //Set the current node to the first Polygon of the group
                if ((pGroup as NodeWithName).Children.Count != 0)
                {
                    _currentSelectedNode = (pGroup as NodeWithName).Children[0] as Polygon;
                    ((pGroup as PolygonGroup).Children[0] as Polygon).IsSelected = true;
                }
                else
                {
                    (pGroup as PolygonGroup).IsSelected = true;
                    _currentSelectedNode = (pGroup as NodeWithName);
                }
            }
            //pretty the same as the above just with the currentselected as the polygon itself so you don't have to go back up to the parent Polygroup
            else if (_currentSelectedNode is Polygon)
            {
                //Close all the previous children
                var parentFrame = ((_currentSelectedNode as Polygon).Parent as PolygonGroup).Parent as ImageFrame;
                if (parentFrame != null)
                {
                    parentFrame.Expanded = false;
                    foreach (var child in parentFrame.Children)
                    {
                        (child as NodeWithName).Expanded = false;
                        (child as NodeWithName).IsSelected = false;
                    }
                }

                var index = (parentFrame.Parent as ImageData).Children.IndexOf(parentFrame);
                ((parentFrame.Parent as ImageData).Children[index] as
                    ImageFrame).IsSelected = false;
                ((parentFrame.Parent as ImageData).Children[index] as
                    ImageFrame).Expanded = false;

                if (index - 1 >= 0)
                {
                    --index;
                }

                _viewModelLocator.ImageFrameView.Frame = ((parentFrame.Parent as ImageData).Children[index] as ImageFrame);
                _viewModelLocator.ImageFrameView.Frame.Expanded = true;
                _viewModelLocator.ImageFrameView.Polygon = null;
                _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = true;
                _viewModelLocator.ImageFrameView.ShowPolygonTextBox = false;
                CurrentView = _viewModelLocator.ImageFrameView;
                var pGroup = _viewModelLocator.ImageFrameView.Frame.Children.First(t => t.Name == _lastPolygonGroupName);

                (pGroup as PolygonGroup).Expanded = true;

                //double check if there is a polygon to set onto next frame else just select the group
                if ((pGroup as PolygonGroup).Children.Count != 0)
                {
                    ((pGroup as PolygonGroup).Children[0] as Polygon).IsSelected = true;
                    _currentSelectedNode = (pGroup as NodeWithName).Children[0] as Polygon;
                }
                else
                {
                    (pGroup as PolygonGroup).IsSelected = true;
                    _currentSelectedNode = (pGroup as NodeWithName);
                }
                _viewModelLocator.ImageFrameView.PolygonGroup = pGroup as PolygonGroup;

            }
        }

        //when we hit enter in the mainwindow and we are on a pgroup we want to jump to the next frame and open the same p group
        public void JumpToNextImageFrame()
        {
            //if we have an polygroup selected go up to the parent imagedata and go to the next child frame and then open to the polygroup.
            if (_currentSelectedNode is PolygonGroup)
            {
                //Close all the previous children
                var parentFrame = ((_currentSelectedNode as PolygonGroup).Parent as ImageFrame);
                if ( parentFrame != null)
                {
                    parentFrame.Expanded = false;
                    foreach (var child in parentFrame.Children)
                    {
                        (child as NodeWithName).Expanded = false;
                        (child as NodeWithName).IsSelected = false;
                    }
                }

                //Get the index of the current Frame from the ImageData and close and deselect the imageFrame
                var index = (parentFrame.Parent as ImageData).Children.IndexOf(parentFrame);
                ((parentFrame.Parent as ImageData).Children[index] as
                    ImageFrame).IsSelected = false;
                ((parentFrame.Parent as ImageData).Children[index] as
                    ImageFrame).Expanded = false;

                //Check to see if there is another frame or not
                if (index + 1 < ((parentFrame.Parent as ImageData).Children.Count))
                {
                    ++index;
                }

                //Set the viewmodellocator varialbes and Open the next Frame
                _viewModelLocator.ImageFrameView.Frame = ((parentFrame.Parent as ImageData).Children[index] as ImageFrame);
                _viewModelLocator.ImageFrameView.Frame.Expanded = true;
                _viewModelLocator.ImageFrameView.Polygon = null;
                _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = true;
                _viewModelLocator.ImageFrameView.ShowPolygonTextBox = false;
                CurrentView = _viewModelLocator.ImageFrameView;
                var pGroup = _viewModelLocator.ImageFrameView.Frame.Children.First(t => t.Name == _lastPolygonGroupName);

                (pGroup as PolygonGroup).Expanded = true;
                
                _viewModelLocator.ImageFrameView.PolygonGroup = pGroup as PolygonGroup;
                //Set the current node to the first Polygon of the group
                if((pGroup as NodeWithName).Children.Count != 0)
                {
                    _currentSelectedNode = (pGroup as NodeWithName).Children[0] as Polygon;
                    ((pGroup as PolygonGroup).Children[0] as Polygon).IsSelected = true;
                }
                else
                {
                    (pGroup as PolygonGroup).IsSelected = true;
                    _currentSelectedNode = (pGroup as NodeWithName);
                }
            }
                //pretty the same as the above just with the currentselected as the polygon itself so you don't have to go back up to the parent Polygroup
            else if (_currentSelectedNode is Polygon)
            {
                //Close all the previous children
                var parentFrame = ((_currentSelectedNode as Polygon).Parent as PolygonGroup).Parent as ImageFrame;
                if (parentFrame != null)
                {
                    parentFrame.Expanded = false;
                    foreach (var child in parentFrame.Children)
                    {
                        (child as NodeWithName).Expanded = false;
                        (child as NodeWithName).IsSelected = false;
                    }
                }

                var index = (parentFrame.Parent as ImageData).Children.IndexOf(parentFrame);
                ((parentFrame.Parent as ImageData).Children[index] as
                    ImageFrame).IsSelected = false;
                ((parentFrame.Parent as ImageData).Children[index] as
                    ImageFrame).Expanded = false;

                if (index + 1 < ((parentFrame.Parent as ImageData).Children.Count))
                {
                    ++index;
                }

                _viewModelLocator.ImageFrameView.Frame = ((parentFrame.Parent as ImageData).Children[index] as ImageFrame);
                _viewModelLocator.ImageFrameView.Frame.Expanded = true;
                _viewModelLocator.ImageFrameView.Polygon = null;
                _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = true;
                _viewModelLocator.ImageFrameView.ShowPolygonTextBox = false;
                CurrentView = _viewModelLocator.ImageFrameView;
                var pGroup = _viewModelLocator.ImageFrameView.Frame.Children.First(t => t.Name == _lastPolygonGroupName);

                (pGroup as PolygonGroup).Expanded = true;

                //double check if there is a polygon to set onto next frame else just select the group
                if((pGroup as PolygonGroup).Children.Count != 0)
                {
                    ((pGroup as PolygonGroup).Children[0] as Polygon).IsSelected = true;
                    _currentSelectedNode = (pGroup as NodeWithName).Children[0] as Polygon;
                }
                else
                {
                    (pGroup as PolygonGroup).IsSelected = true;
                    _currentSelectedNode = (pGroup as NodeWithName);
                }
                _viewModelLocator.ImageFrameView.PolygonGroup = pGroup as PolygonGroup;
                
            }
        }

        public SmartCommand<object> SelectedItemChangedCommand { get; private set; }

        public bool CanExecuteSelectedItemChangedCommand(object o)
        {
            return true;
        }

        public void ExecuteSelectedItemChangedCommand(object o)
        {
            var e = o as RoutedPropertyChangedEventArgs<object>;
            //To enable Delete key to remove nodes, we will store the last item you clicked on 
            //cause you won't try to delete something without clicking on it anyways

            if (e.NewValue is Document)
            {
                _viewModelLocator.DocumentView.Document = e.NewValue as Document;
                CurrentView = _viewModelLocator.DocumentView;
                _currentSelectedNode = e.NewValue as Document;
                _currentSelectedNode.Type = "Document";
            }
            else if (e.NewValue is Folder)
            {
                _viewModelLocator.FolderView.Folder = e.NewValue as Folder;
                CurrentView = _viewModelLocator.FolderView;
                _currentSelectedNode = e.NewValue as Folder;
                _currentSelectedNode.Type = "Folder";
            }
            else if (e.NewValue is ImageData)
            {
                _viewModelLocator.ImageView.Image = e.NewValue as ImageData;
                CurrentView = _viewModelLocator.ImageView;
                _currentSelectedNode = e.NewValue as ImageData;
                _currentSelectedNode.Type = "ImageData";
            }
            else if (e.NewValue is ImageFrame)
            {
                _viewModelLocator.ImageFrameView.Frame = e.NewValue as ImageFrame;
                _viewModelLocator.ImageFrameView.PolygonGroup = null;
                _viewModelLocator.ImageFrameView.Polygon = null;
                _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = false;
                _viewModelLocator.ImageFrameView.ShowPolygonTextBox = false;
                CurrentView = _viewModelLocator.ImageFrameView;
                    
                _currentSelectedNode = e.NewValue as ImageFrame;
                _currentSelectedNode.Type = "ImageFrame";

            }
            else if (e.NewValue is PolygonGroup)
            {
                _viewModelLocator.ImageFrameView.Frame = (e.NewValue as PolygonGroup).Parent as ImageFrame;

                //Save the polygroup name that has been last opened, we'll use this to try and Drill down to the 
                //same polygroup name when going to a new frame.
                _viewModelLocator.ImageFrameView.PolygonGroup = e.NewValue as PolygonGroup;
                _lastPolygonGroupName = _viewModelLocator.ImageFrameView.PolygonGroup.Name;
                _viewModelLocator.ImageFrameView.Polygon = null;
                _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = true;
                _viewModelLocator.ImageFrameView.ShowPolygonTextBox = false;
                CurrentView = _viewModelLocator.ImageFrameView;
                _currentSelectedNode = e.NewValue as PolygonGroup;
                _currentSelectedNode.Type = "PolygonGroup";
            }
            else if (e.NewValue is Polygon)
            {
                _viewModelLocator.ImageFrameView.Frame = ((e.NewValue as Polygon).Parent as PolygonGroup).Parent as ImageFrame;
                _viewModelLocator.ImageFrameView.PolygonGroup = ((e.NewValue as Polygon).Parent as PolygonGroup);
                _viewModelLocator.ImageFrameView.Polygon = e.NewValue as Polygon;
                _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = false;
                _viewModelLocator.ImageFrameView.ShowPolygonTextBox = true;
                CurrentView = _viewModelLocator.ImageFrameView;
                _currentSelectedNode = e.NewValue as Polygon;
                _currentSelectedNode.Type = "Polygon";
            }
            else
            {
                CurrentView = null;
            }
        }

        #region Commands

        #region New Doc Command
        public SmartCommand<object> NewDocumentCommand { get; private set; }

        public bool CanExecuteNewDocumentCommand(object o)
        {
            return true;
        }

        public void ExecuteNewDocumentCommand(object o)
        {
            if (Glue.Instance.Document != null && !Glue.Instance.DocumentIsSaved)
            {
                var result = MessageBox.Show("Would you like to save current document before creating another one?", "Save file", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    Glue.Instance.Document.Save(false);
                }
            }
            var document = new Document();
            document.Initialize();

            Glue.Instance.Document = document;
            Glue.Instance.DocumentIsSaved = true;
            Glue.Instance.DocumentIsSaved = false;
            Documents.Clear();
            Documents.Add(Glue.Instance.Document);
        }
        #endregion

        #region Open Doc Command
        public SmartCommand<object> OpenDocumentCommand { get; private set; }

        public bool CanExecuteOpenDocumentCommand(object o)
        {
            return true;
        }

        public void ExecuteOpenDocumentCommand(object o)
        {
            if (Glue.Instance.Document != null && !Glue.Instance.DocumentIsSaved)
            {
                var result = MessageBox.Show("Would you like to save the current project before opening another?", "Save file", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    Glue.Instance.Document.Save(false);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    //They didn't mean to open file
                    return;
                }
            }
            //If they hit cancel in the open dialog, they shouldn't lose all the stuff they had either.
            var doc = Document.Open(Glue);
            if (doc != null)
            {
                Glue.Instance.Document = doc;
                Glue.Instance.DocumentIsSaved = true;
                Documents.Clear();
                Documents.Add(Glue.Instance.Document);
            }
        }
        #endregion

        #region Save Document Command
        public SmartCommand<object> SaveDocumentCommand { get; private set; }

        public bool CanExecuteSaveDocumentCommand(object o)
        {
            return Glue.Instance.Document != null && !Glue.Instance.DocumentIsSaved;
        }

        public void ExecuteSaveDocumentCommand(object o)
        {
            Glue.Instance.Document.Save(false);
        }
        #endregion

        #region Open Pref Command
        public SmartCommand<object> OpenPreferencesWindowCommand { get; private set; }

        public bool CanExecuteOpenPreferencesWindowCommand(object o)
        {
            return true;
        }

        public void ExecuteOpenPreferencesWindowCommand(object o)
        {
            CurrentView = _viewModelLocator.Preferences;
        }
        #endregion

        #region SaveAs Command
        public SmartCommand<object> SaveAsCommand { get; private set; }

        public bool CanExecuteSaveAsCommand(object o)
        {
            return Glue.Instance.Document != null;
        }

        public void ExecuteSaveAsCommand(object o)
        {
            Glue.Instance.Document.Save(true);
        }
        #endregion

        #region Close Command
        public SmartCommand<object> CloseCommand { get; private set; }

        public bool CanExecuteCloseCommand(object o)
        {
            return true;
        }

        public void ExecuteCloseCommand(object o)
        {
            Messenger.Default.Send(new CloseMainWindowMessage());
        }
        #endregion

        #region Export Command
        public SmartCommand<object> ExportCommand { get; private set; }

        public bool CanExecuteExportCommand(object o)
        {
            return Glue.Instance.Document != null;
        }

        public void ExecuteExportCommand(object o)
        {
            var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var export = new DocumentExport(Glue.Instance.Document);
                var json = JsonConvert.SerializeObject(export, new JsonSerializerSettings()
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    Formatting = Formatting.Indented,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });
                File.WriteAllText(dialog.FileName, json);
                MessageBox.Show("Json data has been exported!");
            }
        }
        #endregion

        #region Remove Command
        public SmartCommand<object> RemoveCommand { get; private set; }

        public bool CanExecuteRemoveCommand(object o)
        {
            return _currentSelectedNode != null;
        }

        public void ExecutreRemoveCommand(object o)
        {
            _currentSelectedNode.Remove();
        }
        #endregion

        #region Copy Command
        public SmartCommand<object> CopyCommand { get; private set; }

        public bool CanExecuteCopyCommand(object o)
        {
            return _currentSelectedNode is Polygon;
        }

        public void ExecuteCopyCommand(object o)
        {
            //put the polygon data in a copy variable (as the _currentSelectedNode will end up being whatever you click to paste in)
            _copyPolygon = _currentSelectedNode as Polygon;
        }
        #endregion

        #region Paste Command
        public SmartCommand<object> PasteCommand { get; private set; }

        public bool CanExecutePasteCommand(object o)
        {
            return _currentSelectedNode !=null;
        }

        public void ExecutePasteCommand(object o)
        {
            //Setting the data to the Polygon itself doesn't raise propertyChanged so you have to do it directly to the collection
            //This updates in the view automatically as well.  This only changes the points within the poly and nothing else
            //(makes sense to me that it doesn't change the name and such but that is easy to implement as well if desired)
            var clone = new Polygon().ClonePolygon(_copyPolygon);

            switch (_currentSelectedNode.Type)
            {
                case "Polygon":
                    //set the parent of the clone to the polygongroup that you're pasting into.
                    clone.Parent = _viewModelLocator.ImageFrameView.PolygonGroup;
                        _viewModelLocator.ImageFrameView.Polygon.Children = clone.Children;
                    break;

                case "PolygonGroup":
                    clone.Parent = _viewModelLocator.ImageFrameView.PolygonGroup;
                    if (_viewModelLocator.ImageFrameView.PolygonGroup.Children.Count != 0)
                        _viewModelLocator.ImageFrameView.PolygonGroup.Children[0] = clone;
                    else
                        _viewModelLocator.ImageFrameView.PolygonGroup.Children.Add(clone);
                    break;
                case "ImageFrame":
                    //in the case of wanting to drop a polygon into a image frame, we'll check the Polygroup that it's from and see if there is a 
                    //group that matches, if there is, we then paste the polygon into the first spot, overwriting whatever is there.
                    foreach (var child in _currentSelectedNode.Children)
                    {
                        if (child.Name == clone.Parent.Name)
                        {
                            clone.Parent = child;
                            if(child.Children.Count == 0)
                                child.Children.Add(clone);
                            else
                                child.Children[0] = clone;
                            break;
                        }
                       
                    }
                    break;
            }
        }

        #endregion

        #region Reimport From New Path

        public SmartCommand<object> ReimportFromNewPathCommand { get; private set; }

        public void ExecuteReimportFromNewPathCommand(object o)
        {
            if (_currentSelectedNode is ImageData)
            {
                (_currentSelectedNode as ImageData).ExecuteReimportFromNewPathCommand(null);
            }
        }

        #endregion

        protected override void InitializeCommands()
        {
            NewDocumentCommand = new SmartCommand<object>(ExecuteNewDocumentCommand, CanExecuteNewDocumentCommand);
            SaveDocumentCommand = new SmartCommand<object>(ExecuteSaveDocumentCommand, CanExecuteSaveDocumentCommand);
            OpenDocumentCommand = new SmartCommand<object>(ExecuteOpenDocumentCommand, CanExecuteOpenDocumentCommand);
            SelectedItemChangedCommand = new SmartCommand<object>(ExecuteSelectedItemChangedCommand, CanExecuteSelectedItemChangedCommand);
            OpenPreferencesWindowCommand = new SmartCommand<object>(ExecuteOpenPreferencesWindowCommand, CanExecuteOpenPreferencesWindowCommand);
            SaveAsCommand = new SmartCommand<object>(ExecuteSaveAsCommand, CanExecuteSaveAsCommand);
            CloseCommand = new SmartCommand<object>(ExecuteCloseCommand, CanExecuteCloseCommand);
            ExportCommand = new SmartCommand<object>(ExecuteExportCommand, CanExecuteExportCommand);
            RemoveCommand = new SmartCommand<object>(ExecutreRemoveCommand, CanExecuteRemoveCommand);

            //Copy Paste commands
            CopyCommand = new SmartCommand<object>(ExecuteCopyCommand, CanExecuteCopyCommand);
            PasteCommand = new SmartCommand<object>(ExecutePasteCommand, CanExecutePasteCommand);

            //Reimport Commands
            ReimportFromNewPathCommand = new SmartCommand<object>(ExecuteReimportFromNewPathCommand);
        }

#endregion
    }
}