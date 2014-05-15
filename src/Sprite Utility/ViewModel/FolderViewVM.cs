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
        private ObservableCollection<Folder> _folderList;

        public Folder Folder
        {
            get
            {
                return _folder;
            }
            set
            {
                if (_folder != value)
                {
                    Set(ref _folder, value);
                    var temp = new ObservableCollection<Folder>();
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

        //Stuff for storing/changing parent of image/folders
        public ObservableCollection<Folder> FoldersList
        {
            get
            {
                foreach (Folder folders in ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0].Children)
                {
                    FindFolders(_folderList, new Stack<NodeWithName>(),
                    folders);
                }


                return _folderList;
            }
            set { Set(ref _folderList, value); }
        }

        public void FindFolders(ObservableCollection<Folder> allFolders, Stack<NodeWithName> ancestors, NodeWithName startPoint)
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
            _folderList = new ObservableCollection<Folder>();
        }
    }
}
