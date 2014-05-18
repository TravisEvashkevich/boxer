using System.Diagnostics;
using Boxer.Core;
using Boxer.Services;
using Newtonsoft.Json;

namespace Boxer.Data
{
    [DebuggerDisplay("Polygon Group '{Name}' ({Children.Count} children)")]
    public sealed class PolygonGroup : NodeWithName
    {
        public bool CheckAgainstOtherGroup(PolygonGroup otherGroup)
        {
            //if the names aren't the same we shouldn't even compare kids
            if (Name != otherGroup.Name)
                return false;
            //obv, if the count of poly's in the group is different, then it's different.
            if (Children.Count != otherGroup.Children.Count)
                return false;
            //check each poly using the same index because if the polys are in a different order then like above, it's diff
            for (int index = 0; index < Children.Count; index++)
            {
                Polygon child = Children[index] as Polygon;
                Polygon otherChild = otherGroup.Children[index] as Polygon;

                //check to see if they are the same polygroup
                if (child.Name == otherChild.Name)
                {
                    if (child.Children.Count == 0 && otherChild.Children.Count == 0)
                    {
                        return true;
                    }
                    //then we compare all the children within that polygon (after doing a count check first)
                    if (child.Children.Count != otherChild.Children.Count)
                    {
                        return false;
                    }

                    //We'll compare both points with one forloop because either way, if the points have different coords but have the same
                    //count, then it's different anyways therefore needs user INTERVENTION (damn alcholic polyPoints...)
                    for (int i = 0; i < child.Children.Count; i++)
                    {
                        PolyPoint point = child.Children[i] as PolyPoint;

                        PolyPoint otherPoint = otherChild.Children[i] as PolyPoint;

                        if (point.X != otherPoint.X || point.Y != otherPoint.Y)
                        {
                            return false;
                        }
                        
                        //if it gets through even once that the points aren't the same then we should just stop 
                        //and say it's not the same therefore it needs to be reviewed.
                    }
                }
            }
            return true;
        }



        [JsonProperty("polygons")]
        public override FastObservableCollection<INode> Children
        {
            get
            {
                return _children;
            }
            set
            {
                Set(ref _children, value);
            }
        }

        public PolygonGroup(string name = "New Polygon Group")
        {
            Name = name;
            Children = new FastObservableCollection<INode>();
        }

        #region newPolyCommand
        [JsonIgnore]
        public SmartCommand<object> NewPolygonCommand { get; private set; }
        public bool CanExecuteNewPolygonCommand(object o)
        {
            return true;
        }
        public void ExecuteNewPolygonCommand(object o)
        {
            var polygonGroup = new Polygon();
            AddChild(polygonGroup);
        }
        #endregion

        #region CleanGroupComand

        public SmartCommand<object> CleanGroupCommand { get; private set; }

        public bool CanExecuteCleanGroupCommand(object o)
        {
            return !(Children == null && Children.Count == 0);
        }

        public void ExecuteCleanGroupCommand(object o)
        {
            TraceService.Clean(this);
        }
        #endregion

        protected override void InitializeCommands()
        {
            NewPolygonCommand = new SmartCommand<object>(ExecuteNewPolygonCommand, CanExecuteNewPolygonCommand);
            CleanGroupCommand = new SmartCommand<object>(ExecuteCleanGroupCommand, CanExecuteCleanGroupCommand);
            base.InitializeCommands();
        }
    }
}
