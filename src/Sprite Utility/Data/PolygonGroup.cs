using System.Diagnostics;
using System.Windows.Forms;
using Boxer.Core;
using Boxer.Services;
using Newtonsoft.Json;

namespace Boxer.Data
{
    [DebuggerDisplay("Polygon Group '{Name}' ({Children.Count} children)")]
    public sealed class PolygonGroup : NodeWithName
    {
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
