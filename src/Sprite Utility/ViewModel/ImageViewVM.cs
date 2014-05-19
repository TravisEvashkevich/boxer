using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Boxer.Core;
using Boxer.Data;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.ViewModel
{
    public class ImageViewVM : MainViewModel
    {
        private ImageData _image;
        private ObservableCollection<NodeWithName> _folderList;
        private INode _selectedFolder;

        public ImageData Image
        {
            get { return _image; }
            set { Set(ref _image, value); }
        }

        //Stuff for storing/changing parent of image/folders
        public ObservableCollection<NodeWithName> FoldersList
        {
            get
            {
                _folderList.Clear();
                foreach (Folder folders in ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0].Children)
                {
                    _folderList.Add(folders);
                    FindFolders(_folderList, new Stack<NodeWithName>(), folders);
                }


                return _folderList;
            }
            set { Set(ref _folderList, value); }
        }

        public void FindFolders(ObservableCollection<NodeWithName> allFolders, Stack<NodeWithName> ancestors, NodeWithName startPoint)
        {
            ancestors.Push(startPoint);
            if (startPoint.Children != null && startPoint.Children.Count > 0)
            {
                foreach (var child in startPoint.Children)
                {
                    if (child.Type == "Folder" && !ImageIsChildOfFolder(Image, child as Folder) )
                    {
                        allFolders.Add(child as Folder);
                        FindFolders(allFolders, ancestors, child as NodeWithName);
                    }
                }
            }

            ancestors.Pop();
        }

        private bool ImageIsChildOfFolder(ImageData image, Folder parentFolder)
        {
            foreach (var child in parentFolder.Children)
            {
                if (child is ImageData && child.Name == image.Name)
                {
                    return true;
                }
            }
            return false;
        }

        public INode SelectedFolder
        {
            get { return null; }
            set
            {
                if (value != null && _selectedFolder != value)
                {
                    //time to change the parent of the folder
                    //First we remove it from the current parents children
                    //then we add it to the new parent children
                    value.Children.Add(Image);

                    var index = Image.Parent.Children.IndexOf(Image);
                    if (index != -1)
                    {
                        Image.Parent.Children.RemoveAt(index);
                    }
                    Image.Parent = value;
                    Glue.Instance.DocumentIsSaved = false;
                }
                Set(ref _selectedFolder, value);
                _selectedFolder = null;
            }
        }


        protected override void InitializeCommands()
        {
            _folderList = new ObservableCollection<NodeWithName>();
        }
    }
}
