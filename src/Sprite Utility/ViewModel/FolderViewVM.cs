using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Boxer.Core;
using Boxer.Data;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.ViewModel
{
    public class FolderViewVM : MainViewModel
    {
        private Folder _folder;
        private ObservableCollection<NodeWithName> _folderList;
        private INode _selectedFolder;

        public Folder Folder
        {
            get
            {
                return _folder;
            }
            set
            {
                //only update the folder list when the folder actually changes.
                if (_folder != value)
                {
                    _folderList.Clear();
                    Set(ref _folder, value);
                    var temp = new ObservableCollection<NodeWithName>();
                    if (Folder.Parent != ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0])
                        temp.Add(ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0]);
                    
                    foreach (Folder folders in ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0].Children)
                    {
                        FindFolders(temp, new Stack<NodeWithName>(), folders);
                    }

                    FoldersList = temp;
                    return;
                }
                Set(ref _folder, value);               
            }
        }

        public INode SelectedFolder
        {
            get { return null; }
            set
            {
                if (value != null &&_selectedFolder != value)
                {
                    //time to change the parent of the folder
                    //First we remove it from the current parents children
                    //then we add it to the new parent children
                    value.Children.Add(Folder);

                    var index = Folder.Parent.Children.IndexOf(Folder);
                    if (index != -1)
                    {
                        Folder.Parent.Children.RemoveAt(index);
                    }
                    Folder.Parent = value;
                }
                Set(ref _selectedFolder, value);
                _selectedFolder = null;
            }
        }


        //Stuff for storing/changing parent of image/folders
        public ObservableCollection<NodeWithName> FoldersList
        {
            get
            {
                _folderList.Clear();
                if (Folder.Parent != ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0])
                    _folderList.Add(ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0]);
                foreach (Folder folders in ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0].Children)
                {
                    if(folders != Folder)
                        _folderList.Add(folders);
                    FindFolders(_folderList, new Stack<NodeWithName>(),folders);
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
                    if (child.Type == "Folder" && !FolderIsChildOf(Folder,child as Folder) && !child.Name.Contains(Folder.Name))
                    {
                        allFolders.Add(child as Folder);
                        FindFolders(allFolders, ancestors, child as NodeWithName);
                    }
                }
            }

            ancestors.Pop();
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

        protected override void InitializeCommands()
        {
            _folderList = new ObservableCollection<NodeWithName>();
        }
    }
}
