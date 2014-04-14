using Newtonsoft.Json;

namespace Boxer.Data
{
    public sealed class Polygon : NodeWithName
    {
        [JsonProperty("points")]
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

        public Polygon()
        {
            Name = "New Polygon";
            Children = new FastObservableCollection<INode>();
        }
    }
}
