﻿using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Boxer.Core;
using Newtonsoft.Json;

namespace Boxer.Data
{
    [Serializable]
    public abstract class NodeWithName : MainViewModel, INode
    {
        protected string _name;

        [JsonProperty("name")]
        public virtual string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        public override void Set<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            Glue.Instance.DocumentIsSaved = false;
            base.Set(ref field, value, name);
        }

        protected override void InitializeCommands()
        {
            RemoveCommand = new SmartCommand<object>(ExecuteRemoveCommand, CanExecuteRemoveCommand);
        }

        protected ObservableCollection<INode> _children;

        public string Type { get; set; }
        public  virtual ObservableCollection<INode> Children { get { return _children; } set { Set(ref _children, value); } }

        public INode Parent { get; set; }

        public void Remove()
        {
            if (Parent != null)
                Parent.Children.Remove(this);
        }

        public void AddChild(INode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        [JsonIgnore]
        public SmartCommand<object> RemoveCommand { get; private set; }

        public bool CanExecuteRemoveCommand(object o)
        {
            return true;
        }

        public void ExecuteRemoveCommand(object o)
        {
            Remove();
        }
    }
}