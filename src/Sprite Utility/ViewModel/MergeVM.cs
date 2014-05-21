using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boxer.Core;
using Boxer.Data;
using Boxer.Data.Formats;
using Boxer.Views;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.ViewModel
{
    public class MergeVM : MainWindowVM
    {
        private ObservableCollection<NodeWithName> _needsToBeChecked;
        private List<ImageData> _originals;
        private ObservableCollection<NodeWithName> _noDuplicatesFound;

        private bool _allSelected = false;
        public bool AllSelected { get { return _allSelected; } set { Set(ref _allSelected, value); } }

        private static readonly FileFormat Format = new BinaryFileFormat();

        public ObservableCollection<NodeWithName> NoDuplicatesFound { get { return _noDuplicatesFound; } }

        public ObservableCollection<NodeWithName> NeedsToBeChecked { get { return _needsToBeChecked; } }

        private NodeWithName _selectedItem;
        public NodeWithName SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                Set(ref _selectedItem, value);
                AllSelected = false;
            }
        }

        //This is used for the Merge Function/view
        public SmartCommand<object> MergeCommand { get; private set; }

        public void ExecuteMergeCommand(object o)
        {

            if (_needsToBeChecked == null)
                _needsToBeChecked = new ObservableCollection<NodeWithName>();
            if (_noDuplicatesFound == null)
                _noDuplicatesFound = new ObservableCollection<NodeWithName>();
            if (_originals == null)
                _originals = new List<ImageData>();

            _needsToBeChecked.Clear();
            _noDuplicatesFound.Clear();
            _originals.Clear();

            //for starters we only use one file at a time but for extensibility (quicker later) mid as well make it ready to take multiple 
            var strings = o as string[];
            var doc = new List<Document>();
            foreach (var s in strings)
            {
                doc.Add(Format.Load(s));
            }

            var existingDoc = ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0];

            //We are going to compare the files in binary before we do anything because this will save time in the long run if there is no difference. 
            var isSame = FileEquals(existingDoc.Filename, strings[0]);

            if (!isSame)
            {
                //We flatten both trees to loop over and check the items in them.
                var flattenedExisting = Flatten(existingDoc.Children).AsParallel().ToList();
                var flattenedIncoming = Flatten(doc[0].Children).AsParallel().ToList();

                PopulateNewContent(flattenedExisting, flattenedIncoming);
                PopulateExistingContent(flattenedIncoming, flattenedExisting);
            }
            else
            {
                MessageBox.Show("There is no differences between these files. Please select files that have differences for merging (It will be more effective :D )");
            }
            OpenMergeWindow();
        }

        private void PopulateExistingContent(List<INode> flattenedIncoming, List<INode> flattenedExisting)
        {
            foreach (var incomingNode in flattenedIncoming)
            {
                var data = incomingNode as ImageData;
                if (data != null)
                {
                    foreach (var existingNode in flattenedExisting)
                    {
                        var node = existingNode as ImageData;
                        if (node == null) continue;
                        if (data.Filename != node.Filename) continue;

                        //This is where it gets annoying and long, we have to check all the children to see if they are the same
                        //first we'll do a quick check to see if the frame count is the same, if they are then we'll check to see
                        //if the polygroups in each frame are the same
                        if (incomingNode.Children.Count != existingNode.Children.Count)
                        {
                            _needsToBeChecked.Add(data);
                            _originals.Add(node);
                        }
                        else
                        {
                            //Now we have to check per frame, all the polygroups to see if they
                            for (int index = 0; index < incomingNode.Children.Count; index++)
                            {
                                var incChild = incomingNode.Children[index];
                                for (int i = index; i < existingNode.Children.Count; )
                                {
                                    var existChild = existingNode.Children[i];
                                    foreach (PolygonGroup incPolyGroup in incChild.Children)
                                    {
                                        foreach (PolygonGroup existingPolyGroup in existChild.Children)
                                        {
                                            if (incPolyGroup.Name == existingPolyGroup.Name)
                                            {
                                                var result = (incPolyGroup).CheckAgainstOtherGroup(existingPolyGroup);
                                                if (!result)
                                                {
                                                    _needsToBeChecked.Add(data);
                                                    _originals.Add(node);
                                                    goto Outer;
                                                }
                                                //since this group was fine, let's check the next one
                                                goto NextGroup;
                                            }
                                        }
                                    NextGroup:
                                        ;
                                    }
                                    //need to get out of the loop so we don't keep comparing the same frame to everything in the inner loop so we jump out
                                    goto NextFrame;
                                }
                            NextFrame:
                                ;
                            }
                        }
                    }
                }
            Outer:
                ;
            }
        }

        private void PopulateNewContent(List<INode> existing, List<INode> incoming)
        {
            //For checking if something exists already or not, we're going to just go on the Folder and Image level so we'll use LINQ
            //To pull the children out of the flattened lists and then loop quickly 

            var existingFolders = existing.Where(i => i.Type == "Folder").ToList().Cast<Folder>();
            var existingImageDatas = existing.Where(i => i.Type == "Image").ToList().Cast<ImageData>();

            var incomingFolders = incoming.Where(i => i.Type == "Folder").ToList().Cast<Folder>();
            var incomingImageDatas = incoming.Where(i => i.Type == "Image").ToList().Cast<ImageData>();

            foreach (var newFolder in incomingFolders.Except(existingFolders, new FolderMergeComparer()))
            {
                _noDuplicatesFound.Add(newFolder);
            }
            foreach (var newImage in incomingImageDatas.Except(existingImageDatas, new ImageDataMergeComparer()))
            {
                _noDuplicatesFound.Add(newImage);
            }
        }

        private class FolderMergeComparer : IEqualityComparer<Folder>
        {
            public bool Equals(Folder x, Folder y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(Folder obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        private class ImageDataMergeComparer : IEqualityComparer<ImageData>
        {
            public bool Equals(ImageData x, ImageData y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(ImageData obj)
            {
                return obj.Name.GetHashCode();
            }
        }

        static IEnumerable<INode> Flatten(IEnumerable<INode> collection)
        {
            foreach (var node in collection)
            {
                yield return node;
                if (node.Children == null) continue;
                foreach (var child in Flatten(node.Children))
                {
                    yield return child;
                }
            }
        }

        #region BinaryCompare
        static bool FileEquals(string fileName1, string fileName2)
        {
            // Check the file size and CRC equality here.. if they are equal...    
            using (var file1 = new FileStream(fileName1, FileMode.Open))
            using (var file2 = new FileStream(fileName2, FileMode.Open))
                return StreamEquals(file1, file2);
        }

        static bool StreamEquals(Stream stream1, Stream stream2)
        {
            const int bufferSize = 2048;
            byte[] buffer1 = new byte[bufferSize]; //buffer size
            byte[] buffer2 = new byte[bufferSize];
            while (true)
            {
                int count1 = stream1.Read(buffer1, 0, bufferSize);
                int count2 = stream2.Read(buffer2, 0, bufferSize);

                if (count1 != count2)
                    return false;

                if (count1 == 0)
                    return true;

                // You might replace the following with an efficient "memcmp"
                if (!buffer1.Take(count1).SequenceEqual(buffer2.Take(count2)))
                    return false;
            }
        }

        #endregion

        #region OpenMergeWindow

        public void OpenMergeWindow()
        {

            //Time to show the window and display stuff.
            MergeWindow merge = new MergeWindow();
            merge.WindowState = WindowState.Maximized;
            merge.Show();
        }


        #endregion

        #region MergeSelectionChanged

        public SmartCommand<object> MergeSelectionChangedCommand { get; private set; }

        public void ExecuteMergeSelectionChangedCommand(object o)
        {
            var e = o as RoutedPropertyChangedEventArgs<object>;
            //To enable Delete key to remove nodes, we will store the last item you clicked on 
            //cause you won't try to delete something without clicking on it anyways
            if (e.OldValue != e.NewValue)

                if (e.NewValue is Folder)
                {
                    SelectedItem = e.NewValue as NodeWithName;
                }
                else if (e.NewValue is ImageData)
                {
                    SelectedItem = e.NewValue as NodeWithName;
                }
                else
                {
                    SelectedItem = null;
                }
        }

        #endregion

        #region KeepSelectedCommand
        public SmartCommand<object> KeepSelectedCommand { get; private set; }

        public bool CanExecuteKeepSelectedCommand(object o)
        {
            return SelectedItem != null;
        }

        public void ExecuteKeepSelectedCommand(object o)
        {
            var main = ServiceLocator.Current.GetInstance<MainWindowVM>();

            if(!AllSelected)
            {
                if (SelectedItem is ImageData && NeedsToBeChecked.Contains(SelectedItem))
                {
                    //if the "keep" refers to a changed data, then we just replace and remove
                    for (int index = 0; index < _originals.Count; index++)
                    {
                        if (_originals[index].Name == SelectedItem.Name)
                        {
                            var parent = _originals[index].Parent;
                            _originals[index].Remove();
                            parent.Children.Add(SelectedItem);
                            NeedsToBeChecked.Remove(SelectedItem);

                            if (NeedsToBeChecked.Count > 0)
                            {
                                SelectedItem = NeedsToBeChecked[0];
                                NeedsToBeChecked[0].IsSelected = true;
                            }
                            else if (NoDuplicatesFound.Count > 0)
                            {
                                SelectedItem = NoDuplicatesFound[0];
                                NoDuplicatesFound[0].IsSelected = true;
                            }
                            else
                            {
                                SelectedItem = null;
                            }

                            _originals.Remove(_originals[index]);

                            return;
                        }
                    }
                }

                //check the document to see if the "merged" folder is made or not.
                bool mergedCreated = main.Documents[0].Children.Any(child => child.Name == "Merged");

                //do stuff based on if merged exists already or not
                var merged = new Folder();
                if (!mergedCreated)
                {
                    merged.Parent = main.Documents[0];
                    main.Documents[0].AddChild(new Folder() { Name = "Merged" });
                }
                merged = main.Documents[0].Children.First(child => child.Name == "Merged") as Folder;
                //add the item to the merged folder OR overwrite the original data

                if (SelectedItem != null)
                {
                    merged.AddChild(SelectedItem);
                    //find and remove the item from the list in Merged
                    NoDuplicatesFound.Remove(SelectedItem);
                    if (NoDuplicatesFound.Count > 0)
                    {
                        SelectedItem = NoDuplicatesFound[0];
                        NoDuplicatesFound[0].IsSelected = true;
                    }
                    else if (NeedsToBeChecked.Count > 0)
                    {
                        SelectedItem = NeedsToBeChecked[0];
                        NeedsToBeChecked[0].IsSelected = true;
                    }
                    else
                    {
                        SelectedItem = null;
                    }
                }
            }
            else
            {
                if (SelectedItem is ImageData && NeedsToBeChecked.Contains(SelectedItem))
                {
                    if(NeedsToBeChecked.Count == _originals.Count)
                    {
                        for (int index = 0; index < _originals.Count; index++)
                        {
                            if (_originals[index].Name == NeedsToBeChecked[index].Name)
                            {
                                var parent = _originals[index].Parent;
                                _originals[index].Remove();
                                parent.Children.Add(NeedsToBeChecked[index]);
                            
                                if (NeedsToBeChecked.Count > 0)
                                {
                                    SelectedItem = NeedsToBeChecked[0];
                                    NeedsToBeChecked[0].IsSelected = true;
                                }
                            }
                        }
                        NeedsToBeChecked.Clear();
                        _originals.Clear();
                    }

                    if (NoDuplicatesFound.Count > 0)
                    {
                        SelectedItem = NoDuplicatesFound[0];
                        NoDuplicatesFound[0].IsSelected = true;
                    }
                    else
                    {
                        SelectedItem = null;
                    }
                }
                else
                {
                    //check the document to see if the "merged" folder is made or not.
                    bool mergedCreated = main.Documents[0].Children.Any(child => child.Name == "Merged");

                    //do stuff based on if merged exists already or not
                    var merged = new Folder();
                    if (!mergedCreated)
                    {
                        merged.Parent = main.Documents[0];
                        main.Documents[0].AddChild(new Folder() { Name = "Merged" });
                    }
                    merged = main.Documents[0].Children.First(child => child.Name == "Merged") as Folder;
                    if(merged != null)
                    {
                        foreach (var nodeWithName in _noDuplicatesFound)
                        {
                            //add the item to the merged folder
                            merged.AddChild(nodeWithName);
                        }
                        NoDuplicatesFound.Clear();
                    }
                }
            }
        }

        #endregion

        #region TrashSelectedCommand

        public SmartCommand<object> TrashSelectedCommand { get; private set; }

        public bool CanExecuteTrashSelectedCommand(object o)
        {
            return SelectedItem != null;
        }

        public void ExecuteTrashSelectedCommand(object o)
        {
            //Just Remove the shit.
            NeedsToBeChecked.Remove(SelectedItem);
            NoDuplicatesFound.Remove(SelectedItem);
        }
        #endregion

        #region SelectAllCommand

        public SmartCommand<object> SelectAllCommand { get; private set; }

        public bool CanExecuteSelectAllCommand(object o)
        {
            return SelectedItem != null;
        }

        public void ExecuteSelectAllCommand(object o)
        {
            AllSelected = true;

        }

        #endregion

        protected override void InitializeCommands()
        {
            MergeCommand = new SmartCommand<object>(ExecuteMergeCommand);
            MergeSelectionChangedCommand = new SmartCommand<object>(ExecuteMergeSelectionChangedCommand);

            KeepSelectedCommand = new SmartCommand<object>(ExecuteKeepSelectedCommand, CanExecuteKeepSelectedCommand);
            TrashSelectedCommand = new SmartCommand<object>(ExecuteTrashSelectedCommand, CanExecuteTrashSelectedCommand);

            SelectAllCommand = new SmartCommand<object>(ExecuteSelectAllCommand, CanExecuteSelectAllCommand);
            base.InitializeCommands();
        }
    }
}
