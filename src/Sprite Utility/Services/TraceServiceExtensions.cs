using System.Collections.Generic;
using System.Linq;
using Boxer.Data;
using FarseerPhysics;
using fwd;

namespace Boxer.Services
{
    public static class TraceServiceExtensions
    {
        public static Shape ToShape(this PolygonGroup polygonGroup)
        {
            var shape = new Shape();
            foreach (var polygon in polygonGroup.Children.Cast<Polygon>())
            {
                var polyPoints = polygon.Children.Cast<PolyPoint>().ToList();
                var vertices = new List<Vector2>(polyPoints.Count);
                vertices.AddRange(polyPoints.Select(point => ConvertUnits.ToSimUnits(new Vector2(point.X, point.Y))));

                shape.Vertices.Add(vertices);
            }
            return shape;
        }
    }
}