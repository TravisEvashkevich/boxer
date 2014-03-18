using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Boxer.Core;
using Boxer.Properties;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Boxer.Data
{
    public sealed class Folder : NodeWithName
    {
        private ObservableCollection<Folder> _folders;
        
        [JsonProperty("folders")]
        public ObservableCollection<Folder> Folders { get { return _folders; } set { Set(ref _folders, value); } }

        private ObservableCollection<ImageData> _images;
        
        [JsonProperty("images")]
        public ObservableCollection<ImageData> Images { get { return _images; } set { Set(ref _images, value); } }

       [JsonIgnore]
        public override ObservableCollection<INode> Children
        {
            get
            {
                return _children; 
            }
            set
            {
                Set(ref _children, value);
            }
        }

        public Folder()
        {
            Folders = new ObservableCollection<Folder>();
            Images = new ObservableCollection<ImageData>();
            Children = new ObservableCollection<INode>();
            Children.CollectionChanged += ChildrenOnCollectionChanged;
            Name = "New Folder";
            Type = "Folder";
        }

        private void ChildrenOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in notifyCollectionChangedEventArgs.NewItems)
                {
                    if (item is ImageData)
                    {
                        Images.Add(item as ImageData);
                    }
                    if (item is Folder)
                    {
                        Folders.Add(item as Folder);
                    }
                }
            }
            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in notifyCollectionChangedEventArgs.OldItems)
                {
                    if (item is ImageData)
                    {
                        Images.Remove(item as ImageData);
                    }
                    if (item is Folder)
                    {
                        Folders.Remove(item as Folder);
                    }
                }
            }
        }

        [JsonConstructor]
        public Folder(ObservableCollection<Folder> folders, ObservableCollection<ImageData> images) : this()
        {
            foreach (var folder in folders)
            {
                AddChild(folder);
            }
            foreach (var image in images)
            {
                AddChild(image);
            }
        }


        [JsonIgnore]
        public SmartCommand<object> NewFolderCommand { get; private set; }

        public bool CanExecuteNewFolderCommand(object o)
        {
            return true;
        }

        public void ExecuteNewFolderCommand(object o)
        {
            var folder = new Folder();
            AddChild(folder);
        }

        [JsonIgnore]
        public SmartCommand<object> NewImageCommand { get; private set; }

        public bool CanExecuteNewImageCommand(object o)
        {
            return true;
        }

        public void ExecuteNewImageCommand(object o)
        {

            var dialog = new OpenFileDialog {Filter = "Image Files (*.png, *.gif)|*.png;*.gif", Multiselect = true};
            var result = dialog.ShowDialog();
            if (result.Value)
            {
                foreach (var filename in dialog.FileNames)
                {
                    var imageData = ImageDataFactory.CreateFromFilename(filename);

                    AddChild(imageData);
                }
            }
        }

        #region AddExistingFolderCommand
         [JsonIgnore]
        public SmartCommand<object> AddExistingFolderCommand { get; private set; }

        public bool CanExecuteAddExistingFolderCommand(object o)
        {
            return true;
        }

        public async void ExecuteAddExistingFolderCommand(object o)
        {
            ImageDataFactory.ImportFromExistingDirectoryDialog(this);
        }

        #endregion

        protected override void InitializeCommands()
        {
            AddExistingFolderCommand = new SmartCommand<object>(ExecuteAddExistingFolderCommand, CanExecuteAddExistingFolderCommand);  
            NewFolderCommand = new SmartCommand<object>(ExecuteNewFolderCommand, CanExecuteNewFolderCommand);
            NewImageCommand = new SmartCommand<object>(ExecuteNewImageCommand, CanExecuteNewImageCommand);
            base.InitializeCommands();
        }
    }
}
