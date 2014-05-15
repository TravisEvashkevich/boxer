using System;
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
            set
            {
                Set(ref _name, value);
                Glue.Instance.DocumentIsSaved = false;
            }
        }

        private bool _isVisible = true;
        [JsonIgnore]
        public virtual bool IsVisible { get { return _isVisible; } set { Set(ref _isVisible, value); } }
        
        private bool _expanded;
        [JsonIgnore]
        public bool Expanded
        {
            get { return _expanded; }
            set
            {
                Set(ref _expanded, value);
            }
        }

        private bool _approved;
        public bool Approved { get { return _approved; } 
            set { Set(ref _approved, value); } }

        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected { get { return _isSelected; } set { Set(ref _isSelected, value); } }

        public override void Set<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            base.Set(ref field, value, name);
        }


        protected FastObservableCollection<INode> _children;
      
        public string Type { get; set; }
        public virtual FastObservableCollection<INode> Children 
        { get { return _children; } 
            set
            {
                Set(ref _children, value);
            Glue.Instance.DocumentIsSaved = false;
        } }

        public INode Parent { get; set; }

        public void Remove()
        {
            if (Parent != null)
            {
                Parent.Children.Remove(this);
                //set the doc to dirty since you removed something
                Glue.Instance.DocumentIsSaved = false;
            }
        }

        public void AddChild(INode child)
        {
            child.Parent = this;
            Children.Add(child);
            Glue.Instance.DocumentIsSaved = false;
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

        protected override void InitializeCommands()
        {
            RemoveCommand = new SmartCommand<object>(ExecuteRemoveCommand, CanExecuteRemoveCommand);
        }
    }
}