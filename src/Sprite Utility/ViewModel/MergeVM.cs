using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boxer.Core;
using Boxer.Data;
using Boxer.Data.Formats;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.ViewModel
{
    public class MergeVM : MainWindowVM
    {
        private List<NodeWithName> _needsToBeChecked;
        private List<NodeWithName> _noDuplicatesFound;
        private static readonly FileFormat Format = new BinaryFileFormat();

        //This is used for the Merge Function/view
        public SmartCommand<object> MergeCommand { get; private set; }

        public void ExecuteMergeCommand(object o)
        {

            if (_needsToBeChecked == null)
                _needsToBeChecked = new List<NodeWithName>();
            if (_noDuplicatesFound == null)
                _noDuplicatesFound = new List<NodeWithName>();

            _needsToBeChecked.Clear();
            _noDuplicatesFound.Clear();

            //Just interested in how long it takes for merge checking
            Stopwatch watch = new Stopwatch();
            watch.Start();

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
                watch.Stop();
                MessageBox.Show("We have different files on our hands boys! Let's get to comparing and merging :D");
                watch.Start();
                //We flatten both trees to loop over and check the items in them.
                var flattenedExisting = Flatten(existingDoc.Children);
                var flattenedIncoming = Flatten(doc[0].Children);

                //For checking if something exists already or not, we're going to just go on the Folder and Image level so we'll use LINQ
                //To pull the children out of the flattened lists and then loop quickly 

                var existingFolders = flattenedExisting.Where(i => i.Type == "Folder");
                var existingImageDatas = flattenedExisting.Where(i => i.Type == "Image");

                var incomingFolders = flattenedIncoming.Where(i => i.Type == "Folder");
                var incomingImageDatas = flattenedIncoming.Where(i => i.Type == "Image");

                //Check for folders in the INCOMING SUF that don't exist in the EXISTING SUF
                CheckFoldersForNonExisting(incomingFolders, existingFolders);
                CheckImagesForNonExisting(incomingImageDatas, existingImageDatas);


                foreach (var incomingNode in flattenedIncoming)
                {
                    if (incomingNode is ImageData)
                    {
                        foreach (var existingNode in flattenedExisting)
                        {
                            if (existingNode is ImageData)
                            {
                                if (((ImageData)incomingNode).Filename == ((ImageData)existingNode).Filename)
                                {
                                    //This is where it gets annoying and long, we have to check all the children to see if they are the same
                                    //first we'll do a quick check to see if the frame count is the same, if they are then we'll check to see
                                    //if the polygroups in each frame are the same
                                    if (incomingNode.Children.Count != existingNode.Children.Count)
                                    {
                                        _needsToBeChecked.Add(incomingNode as NodeWithName);
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
                                                                _needsToBeChecked.Add(incomingNode as NodeWithName);
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
                        }
                    }
                Outer:
                    ;
                }
            }
            else
            {
                MessageBox.Show("There is no differences between these files. Please select files that have differences for merging (It will be more effective :D )");
            }
            string names = "";
            foreach (var nodeWithName in _needsToBeChecked)
            {
                names += nodeWithName.Name;
                names += "\n";
            }
            names += "Folders or Images with No Duplicates.\n";
            foreach (var unique in _noDuplicatesFound)
            {
                names += unique.Name;
                names += "\n";
            }
            watch.Stop();
            MessageBox.Show(string.Format("Finished Merge Process. It took {0} We found {1} difference(s). \n Files that differ are : \n {2}", watch.Elapsed, _needsToBeChecked.Count + _noDuplicatesFound.Count, names));
        }

        private void CheckImagesForNonExisting(IEnumerable<INode> incomingImageDatas, IEnumerable<INode> existingImageDatas)
        {
            //we need to compare (I'm only going to compare the name and children of the folders, not the image childre, just folder children)
            //to see if there is any new folders being brought in. I think it's better to let the user decide if they want to use the folder or not
            //even if it could contain dupes/etc. that's up to the user to handle, we're just telling them that there is a folder that isn't in
            //their current SUF
            foreach (var incomingImage in incomingImageDatas)
            {
                bool exists = false;
                foreach (var existingImage in existingImageDatas)
                {
                    if (incomingImage.Name == existingImage.Name)
                    {
                        exists = true;
                    }

                }
                if (exists) continue;

                if (!_noDuplicatesFound.Contains(incomingImage))
                {
                    //for Images we should probably also check the parent folders to see if we already have the folder in noDupes

                    _noDuplicatesFound.Add(incomingImage as NodeWithName);
                }
            }
        }

        private void CheckFoldersForNonExisting(IEnumerable<INode> incomingFolders, IEnumerable<INode> existingFolders)
        {
            //we need to compare (I'm only going to compare the name and children of the folders, not the image childre, just folder children)
            //to see if there is any new folders being brought in. I think it's better to let the user decide if they want to use the folder or not
            //even if it could contain dupes/etc. that's up to the user to handle, we're just telling them that there is a folder that isn't in
            //their current SUF
            foreach (var incomingFolder in incomingFolders)
            {
                bool exists = false;
                foreach (var existingFolder in existingFolders)
                {
                    if (incomingFolder.Name == existingFolder.Name)
                    {
                        exists = true;
                    }

                }
                if (exists) continue;

                if (!_noDuplicatesFound.Contains(incomingFolder))
                {
                    _noDuplicatesFound.Add(incomingFolder as NodeWithName);
                }
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

        //Feed an incoming node against the document we have, loop through Everything and check to see what is there...this is going to be a looooooooong process I think
        public void CheckForDuplicates(NodeWithName incoming, Stack<NodeWithName> ancestors, NodeWithName startPoint)
        {
            if (IsCriteriaMatched(incoming.Name, startPoint))
            {
                if (incoming is ImageData)
                {
                    //check to see if the image data is the same or not, not sure whether doing a compareTo or equality would be the best and most efficient way to do this.
                    if (incoming == startPoint)
                    {
                        MessageBox.Show(incoming.Name + " is the same as " + startPoint.Name);
                    }
                }
            }
            else
            {
                _noDuplicatesFound.Add(incoming);
            }
            ancestors.Push(startPoint);
            if (startPoint.Children != null && startPoint.Children.Count > 0)
            {
                foreach (var child in incoming.Children)
                    if (child.Type != null && !child.Type.Contains(typeof(ImageFrame).ToString()) &&
                       (child.Type != "Polygon" && child.Type != "PolyPoint" && child.Type != "PolygonGroup"))
                        CheckForDuplicates(incoming, ancestors, child as NodeWithName);
            }

            ancestors.Pop();
        }

        private bool IsCriteriaMatched(string criteria, NodeWithName check)
        {
            return String.IsNullOrEmpty(criteria) || check.Name.ToLower() == (criteria.ToLower()) && check is ImageData;
        }


        protected override void InitializeCommands()
        {
            MergeCommand = new SmartCommand<object>(ExecuteMergeCommand);
            base.InitializeCommands();
        }
    }
}
