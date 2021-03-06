﻿using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using Boxer.Core;
using Boxer.Data.Formats;
using Newtonsoft.Json;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Boxer.Data
{
    public sealed class Document : NodeWithName
    {
        private static readonly FileFormat Format = new BinaryFileFormat();

        public void Save(bool forceNewName)
        {
            if (forceNewName || Filename == "Not Saved")
            {
                var dialog = new SaveFileDialog();
                dialog.Filter = "Sprite Utility Files (*.suf)|*.suf";
                var result = dialog.ShowDialog();
                if (result.Value)
                {
                    Filename = dialog.FileName;
                }
                else
                {
                    return;
                }
            }

            Format.Save(Filename, this);
            Glue.Instance.DocumentIsSaved = true;
            MessageBox.Show("Saved.");
        }

        public static Document Open(Glue glue)
        {
            string fileName;

            var dialog = new OpenFileDialog();
            dialog.Filter = "Sprite Utility Files (*.suf)|*.suf";

            var result = dialog.ShowDialog();
            if (result.Value)
            {
                fileName = dialog.FileName;
            }
            else
            {
                return null;
            }

            Document deserialized = null;
            Parallel.Invoke(() =>
            {
                deserialized = Format.Load(fileName);
                
                Application.DoEvents();
                var dirty = EnsureDefaultsRecursively(deserialized.Children);
                if (dirty)
                {
                    Glue.Instance.DocumentIsSaved = false;
                }
            });

            deserialized.Filename = fileName;
            return deserialized;
        }

        public static bool EnsureDefaultsRecursively(ObservableCollection<INode> nodes, bool dirty = false, bool rebuildAll = false)
        {
            foreach (var node in nodes)
            {
                if (node is ImageData)
                {
                    foreach (var frame in node.Children)
                    {
                        dirty |= ImageDataFactory.EnsureDefaults((ImageFrame)frame, rebuildAll);
                    }
                }

                if (node is Folder)
                {
                    dirty |= EnsureDefaultsRecursively(node.Children, dirty, rebuildAll);
                }
            }

            return dirty;
        }

        [JsonProperty("folders")]
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

        private string _filename;
        public string Filename { get { return _filename; } set { Set(ref _filename, value); } }

        public Document()
        {
            Name = "New Document";
            Filename = "Not Saved";
            Children = new FastObservableCollection<INode>();
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

        #region CleanAllPolygroupCommand
        [JsonIgnore]
        public SmartCommand<object> CleanAllPolygroupCommand { get; private set; }

        public void ExecuteCleanAllPolygroupCommand(object o)
        {
            foreach (var child in Children)
            {
                if (child is Folder)
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
            CleanAllPolygroupCommand = new SmartCommand<object>(ExecuteCleanAllPolygroupCommand, CanExecuteCleanAllPolygroupsCommand);
            base.InitializeCommands();
        }

        private bool CanExecuteCleanAllPolygroupsCommand(object arg)
        {
            return !(Children == null && Children.Count == 0);
        }
    }
}