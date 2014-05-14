﻿using System.Diagnostics;
using Boxer.Core;
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
        
        protected override void InitializeCommands()
        {
            NewPolygonCommand = new SmartCommand<object>(ExecuteNewPolygonCommand, CanExecuteNewPolygonCommand);
            base.InitializeCommands();
        }
    }
}
