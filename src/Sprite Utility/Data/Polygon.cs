using System.Collections.Generic;
using System.Windows;
using Microsoft.Windows;
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

        //Method mainly for the copy paste function, make a clone of the 
        //poly that way when you change a point in one you don't change it for every 
        //copied poly.
        public Polygon ClonePolygon(Polygon toClone)
        {
            var poly = new Polygon();
            poly.Name = toClone.Name;
            foreach (PolyPoint child in toClone.Children)
            {
                var point = new PolyPoint(child.X, child.Y);
                poly.AddChild(point);
            }
            poly.Parent = toClone.Parent;

            return poly;
        }

    }
}
