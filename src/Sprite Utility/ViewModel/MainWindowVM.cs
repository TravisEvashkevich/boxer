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

        public SmartCommand<object> SelectedItemChangedCommand { get; private set; }

        public bool CanExecuteSelectedItemChangedCommand(object o)
        {
            return true;
        }

        public void ExecuteSelectedItemChangedCommand(object o)
        {
            var e = o as RoutedPropertyChangedEventArgs<object>;

            if (e.NewValue is Document)
            {
                _viewModelLocator.DocumentView.Document = e.NewValue as Document;
                CurrentView = _viewModelLocator.DocumentView;
            }
            else if (e.NewValue is Folder)
            {
                _viewModelLocator.FolderView.Folder = e.NewValue as Folder;
                CurrentView = _viewModelLocator.FolderView;
            }
            else if (e.NewValue is ImageData)
            {
                _viewModelLocator.ImageView.Image = e.NewValue as ImageData;
                CurrentView = _viewModelLocator.ImageView;
            }
            else if (e.NewValue is ImageFrame)
            {
                _viewModelLocator.ImageFrameView.Frame = e.NewValue as ImageFrame;

                //If the last selected pgroup isn't null then set it for the newly selected image frame. If a new group is wanted,
                // they will have to manually select it. else do the regular
                if (_lastPolygonGroupName != null)
                {
                    //Have to check to see if the newly selected image frame has the Polygroup that the last image frame did (does the new frame have
                    //Attack or clip etc, then select it if it does.
                    var newImageFrame = e.NewValue as ImageFrame;
                    foreach (INode t in newImageFrame.Children.Where(t => t.Name == _lastPolygonGroupName))
                    {
                        _viewModelLocator.ImageFrameView.PolygonGroup = t as PolygonGroup;
                        break;
                    }

                    _viewModelLocator.ImageFrameView.Polygon = null;
                    _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = true;
                    _viewModelLocator.ImageFrameView.ShowPolygonTextBox = false;
                    CurrentView = _viewModelLocator.ImageFrameView; 
                }
                else
                {
                    _viewModelLocator.ImageFrameView.PolygonGroup = null;
                    _viewModelLocator.ImageFrameView.Polygon = null;
                    _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = false;
                    _viewModelLocator.ImageFrameView.ShowPolygonTextBox = false;
                    CurrentView = _viewModelLocator.ImageFrameView; 
                }

                
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
                
            }
            else if (e.NewValue is Polygon)
            {
                _viewModelLocator.ImageFrameView.Frame = ((e.NewValue as Polygon).Parent as PolygonGroup).Parent as ImageFrame;
                _viewModelLocator.ImageFrameView.PolygonGroup = ((e.NewValue as Polygon).Parent as PolygonGroup);
                _viewModelLocator.ImageFrameView.Polygon = e.NewValue as Polygon;
                _viewModelLocator.ImageFrameView.ShowPolygonGroupTextBox = false;
                _viewModelLocator.ImageFrameView.ShowPolygonTextBox = true;
                CurrentView = _viewModelLocator.ImageFrameView;
            }
            else
            {
                CurrentView = null;
            }
        }

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

        public SmartCommand<object> SaveDocumentCommand { get; private set; }

        public bool CanExecuteSaveDocumentCommand(object o)
        {
            return Glue.Instance.Document != null && !Glue.Instance.DocumentIsSaved;
        }

        public void ExecuteSaveDocumentCommand(object o)
        {
            Glue.Instance.Document.Save(false);
        }

        public SmartCommand<object> OpenPreferencesWindowCommand { get; private set; }

        public bool CanExecuteOpenPreferencesWindowCommand(object o)
        {
            return true;
        }

        public void ExecuteOpenPreferencesWindowCommand(object o)
        {
            CurrentView = _viewModelLocator.Preferences;
        }

        public SmartCommand<object> SaveAsCommand { get; private set; }

        public bool CanExecuteSaveAsCommand(object o)
        {
            return Glue.Instance.Document != null;
        }

        public void ExecuteSaveAsCommand(object o)
        {
            Glue.Instance.Document.Save(true);
        }

        public SmartCommand<object> CloseCommand { get; private set; }

        public bool CanExecuteCloseCommand(object o)
        {
            return true;
        }

        public void ExecuteCloseCommand(object o)
        {
            Messenger.Default.Send(new CloseMainWindowMessage());
        }

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
        }
    }
}