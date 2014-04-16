using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boxer.Core;
using Boxer.Data;

namespace Boxer.ViewModel
{
    public class SearchFilterVM :MainViewModel
    {
        //The string entered in the searchbox
        public string SearchText { get; set; }

        //We will store the original document as well so that way if the search is cleared, 
        //we can reset the view to the original.
        public Document OriginalDocument { get; private set; }

        //this will be the document that has the filtered images and structure in it.
        public Document DesiredDocument { get; set; }

        public SmartCommand<object> SearchCommand { get; private set; }

        public void ExecuteSearchCommand(object o)
        {
            //search the document(s) for things containing the searchText
            
        }

        protected override void InitializeCommands()
        {
            SearchCommand = new SmartCommand<object>(ExecuteSearchCommand);
        }
    }
}
