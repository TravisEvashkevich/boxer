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

        private string _searchText;

        //The string entered in the searchbox
        public string SearchText { get { return _searchText; } set { Set(ref _searchText, value); } }


        public SmartCommand<object> SearchCommand { get; private set; }

        public void ExecuteSearchCommand(object o)
        {
            var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
            if (instance.Documents == null)
                return;
            
            ApplyCriteria(SearchText,new Stack<NodeWithName>(),instance.Documents[0] );
            //instance.Documents[0].IsExpanded = true;
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
            SearchCommand = new SmartCommand<object>(ExecuteSearchCommand);
        }

    }
}
