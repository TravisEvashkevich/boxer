using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Boxer.Core;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Boxer.Data
{
    public sealed class Folder : NodeWithName
    {
        private FastObservableCollection<Folder> _folders;
        
        [JsonProperty("folders")]
        public FastObservableCollection<Folder> Folders { get { return _folders; } set { Set(ref _folders, value); } }

        private FastObservableCollection<ImageData> _images;
        
        [JsonProperty("images")]
        public FastObservableCollection<ImageData> Images { get { return _images; } set { Set(ref _images, value); } }

       [JsonIgnore]
        public override FastObservableCollection<INode> Children
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
            Folders = new FastObservableCollection<Folder>();
            Images = new FastObservableCollection<ImageData>();
            Children = new FastObservableCollection<INode>();
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
        
        [JsonIgnore]
        public SmartCommand<object> NewFolderCommand { get; private set; }
        public bool CanExecuteNewFolderCommand(object o)
        {
            return true;
        }
        public void ExecuteNewFolderCommand(object o)
        {
            var folder = new Folder();
            folder.Initialize();
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

        public void ExecuteAddExistingFolderCommand(object o)
        {
            ImageDataFactory.ImportFromExistingDirectoryDialog(this);
        }

        #endregion

        #region CleanImagePolygroupsCommand
        [JsonIgnore]
        public SmartCommand<object> CleanImagePolygroupsCommand { get; private set; }

        public void ExecuteCleanImagePolygroupsCommand(object o)
        {
            if(Children == null)return;

            foreach (var child in Children)
            {
                if (child is ImageData)
                {
                    (child as ImageData).ExecuteCleanPolygroupsCommand(null);
                }
                else if (child is Folder)
                {
                    (child as Folder).ExecuteCleanImagePolygroupsCommand(null);
                }
            }
        }
        #endregion

        protected override void InitializeCommands()
        {
            AddExistingFolderCommand = new SmartCommand<object>(ExecuteAddExistingFolderCommand, CanExecuteAddExistingFolderCommand);  
            NewFolderCommand = new SmartCommand<object>(ExecuteNewFolderCommand, CanExecuteNewFolderCommand);
            NewImageCommand = new SmartCommand<object>(ExecuteNewImageCommand, CanExecuteNewImageCommand);
            CleanImagePolygroupsCommand = new SmartCommand<object>(ExecuteCleanImagePolygroupsCommand);
            base.InitializeCommands();
        }
    }
}
