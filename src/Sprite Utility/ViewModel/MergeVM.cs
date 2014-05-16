using System;
using System.Collections.Generic;
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
        private static readonly FileFormat Format = new BinaryFileFormat();

        //This is used for the Merge Function/view
        public SmartCommand<object> MergeCommand { get; private set; }

        public void ExecuteMergeCommand(object o)
        {
            //for starters we only use one file at a time but for extensibility (quicker later) mid as well make it ready to take multiple 
            var strings = o as string[];
            var doc = new List<Document>();
            foreach (var s in strings)
            {
                doc.Add(Format.Load(s));
            }
            /*
             * after reading in the files and loading them through the binaryFileFormat, we begin the oh so troublesome 
             * and arduous journey of merging.
             * Step 1. Loop through images and check their names
             * Step 2. If names match, do a compare to see if they are the same or not
             * Step 3. If they are not the same, add to the mergeCheckList
             * Step 4. If item is not a match to anything, Create a new folder in the doc root called "Merged" and then add the folder structure up from the image to that folder
             * Step 5. When all items have been checked, Present the MergedWindow to the user and get results of "check box = overwrite"
             * Step 6.  ????
             * Step 7. Profit??
             */
            var existingDoc = ServiceLocator.Current.GetInstance<MainWindowVM>().Documents[0];



            MessageBox.Show(doc[0].Name);
        }

        public void FindMatches(string criteria, string fullPath, Stack<NodeWithName> ancestors, NodeWithName startPoint)
        {
            if (IsCriteriaMatched(criteria, startPoint))
            {
                if (startPoint is ImageData)
                {
                    (startPoint as ImageData).ReimportImageData(fullPath);

                    MessageBox.Show(String.Format("We reimported {0} successfully.", Path.GetFileNameWithoutExtension(criteria)));

                }
            }

            ancestors.Push(startPoint);
            if (startPoint.Children != null && startPoint.Children.Count > 0)
            {
                foreach (var child in startPoint.Children)
                    if (child.Type != null && !child.Type.Contains(typeof(ImageFrame).ToString()) &&
                       (child.Type != "Polygon" && child.Type != "PolyPoint" && child.Type != "PolygonGroup"))
                        FindMatches(criteria, fullPath, ancestors, child as NodeWithName);
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
