using System.Collections.Generic;
using fwd;

namespace Boxer.Services
{
    public class Shape
    {
        public List<List<Vector2>> Vertices;
        public Shape()
        {
            Vertices = new List<List<Vector2>>();
        }
    }
}