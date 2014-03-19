using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Boxer.Core;
using Newtonsoft.Json;
using JsonSerializer = Boxer.Core.JsonSerializer;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Boxer.Data
{
    public sealed class Document : NodeWithName
    {
        public void Save(bool forceNewName)
        {
            if (forceNewName || Filename == "Not Saved")
            {
                SaveFileDialog Dialog = new SaveFileDialog();
                Dialog.Filter = "Sprite Utility Files (*.suf)|*.suf";
                var result = Dialog.ShowDialog();
                if (result.Value)
                {
                    Filename = Dialog.FileName;
                }
                else
                {
                    return;
                }
            }

            var json = JsonSerializer.Serialize(this);
            File.WriteAllText(Filename, json);

            Glue.DocumentIsSaved = true;
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
                var json = File.ReadAllText(fileName);
                deserialized = JsonSerializer.Deserialize<Document>(json);
                Application.DoEvents();
                var dirty = EnsureDefaultsRecursively(deserialized.Children);
                if (dirty)
                {
                    glue.DocumentIsSaved = false;
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

        private string _filename;

        public string Filename { get { return _filename; } set { Set(ref _filename, value); } }

        public Document()
        {
            Name = "New Document";
            Filename = "Not Saved";
            Children = new ObservableCollection<INode>();
        }

         [JsonConstructor]
        public Document(ObservableCollection<Folder> folders)
            : this()
        {
            foreach (var folder in folders)
            {
                AddChild(folder);
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
            base.InitializeCommands();
        }

    }
}
