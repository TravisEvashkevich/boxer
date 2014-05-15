using System.Collections.Generic;
using System.Collections.ObjectModel;
using Boxer.Core;
using Boxer.Data;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.ViewModel
{
    public class ImageViewVM : MainViewModel
    {
        private ImageData _image;

        public ImageData Image
        {
            get { return _image; }
            set { Set(ref _image, value); }
        }

        //Stuff for storing/changing parent of image/folders
        public ObservableCollection<Folder> FoldersList
        {
            get
            {
                var names = new ObservableCollection<Folder>();

                FindFolders(names, new Stack<NodeWithName>(),
                    ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0]);

                return names;
            }
        }

        public void FindFolders(ObservableCollection<Folder> allFolders, Stack<NodeWithName> ancestors, NodeWithName startPoint)
        {
            ancestors.Push(startPoint);
            if (startPoint.Children != null && startPoint.Children.Count > 0)
            {
                foreach (var child in startPoint.Children)
                    if (child.Type == "Folder")
                    {
                        allFolders.Add(child as Folder);
                        FindFolders(allFolders, ancestors, child as NodeWithName);
                    }
            }

            ancestors.Pop();
        }

        protected override void InitializeCommands()
        {
            
        }
    }
}
