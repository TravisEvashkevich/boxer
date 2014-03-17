using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boxer.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using JsonSerializer = Boxer.Core.JsonSerializer;

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

        public static Document Open()
        {
            string fileName;

            var dialog = new OpenFileDialog();
            dialog.Filter = "Sprite Utility Files (*.suf)|*.suf";
            
            var result = dialog.ShowDialog();
            if (result.Value)
                fileName = dialog.FileName;
            else
                return null;

            var json = File.ReadAllText(fileName);
            var deserialized = JsonSerializer.Deserialize<Document>(json);

            deserialized.Filename = fileName;
           // SetParentRecursivly(deserialized);

            return deserialized;
        }

        public static void SetParentRecursivly(INode node)
        {
            foreach (var child in node.Children)
            {
                child.Parent = node;
                SetParentRecursivly(child);
            }
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
