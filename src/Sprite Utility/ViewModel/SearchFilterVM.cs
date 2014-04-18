using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boxer.Core;
using Boxer.Data;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.ViewModel
{
    public class SearchFilterVM : MainViewModel
    {
        private bool _bFirstSearch = true;
        private string _searchText;
        private Document _desiredDocument;
        //The string entered in the searchbox
        public string SearchText { get { return _searchText; } set { Set(ref _searchText, value); } }

        //We will store the original document as well so that way if the search is cleared, 
        //we can reset the view to the original.
        public Document OriginalDocument { get; private set; }

        //this will be the document that has the filtered images and structure in it.
        public Document DesiredDocument
        {
            get { return _desiredDocument; }
            set
            {
                if (_desiredDocument == null)
                {
                    _desiredDocument = new Document();
                }
                Set(ref _desiredDocument, value);
            }
        }

        public SmartCommand<object> SearchCommand { get; private set; }

        public void ExecuteSearchCommand(object o)
        {
            var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
            if (instance.Documents == null)
                return;
            
            if (_bFirstSearch)
            {
                //store the original doc
                OriginalDocument = instance.Documents[0];
            }
            _bFirstSearch = false;

            ApplyCriteria(SearchText,new Stack<NodeWithName>(),instance.Documents[0] );
            instance.Documents[0].IsExpanded = true;
        }

        private bool IsCriteriaMatched(string criteria, NodeWithName check)
        {
            return String.IsNullOrEmpty(criteria) || check.Name.Contains(criteria);
        }

        public void ApplyCriteria(string criteria, Stack<NodeWithName> ancestors,NodeWithName startPoint)
        {
           if (IsCriteriaMatched(criteria,startPoint))
            {
                startPoint.IsVisible = true;
                foreach (var ancestor in ancestors)
                {
                    ancestor.IsVisible = true;
                    ancestor.IsExpanded = !String.IsNullOrEmpty(criteria);
                }
            }
            else
                startPoint.IsVisible = false;

            ancestors.Push(startPoint);
            foreach (var child in startPoint.Children)
                if(child.Type != null && !child.Type.Contains(typeof(ImageFrame).ToString() )&& 
                    !child.Type.Contains(typeof(Polygon).ToString()) )
                ApplyCriteria(criteria, ancestors,child as NodeWithName);

            ancestors.Pop();
        }

        protected override void InitializeCommands()
        {
            _desiredDocument = new Document();
            SearchCommand = new SmartCommand<object>(ExecuteSearchCommand);
        }

        public void ResetDocument()
        {
            //search the document(s) for things containing the searchText
            //store the original doc
            var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
            instance.Documents[0] = OriginalDocument;

        }
    }
}
