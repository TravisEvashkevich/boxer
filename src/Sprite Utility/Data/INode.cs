namespace Boxer.Data
{
    public interface INode : IName
    {
        string Type { get; set; }
        FastObservableCollection<INode> Children { get; set; }
        INode Parent { get; set; }
        void Initialize();
    }
}
