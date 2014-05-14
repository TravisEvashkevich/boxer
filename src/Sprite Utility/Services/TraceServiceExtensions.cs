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

        // http://blog.csharphelper.com/2010/01/04/determine-whether-a-polygon-is-convex-in-c.aspx
        public static bool IsConvex(this List<Vector2> polygon)
        {
            var points = polygon.ToArray();
            var gotNegative = false;
            var gotPositive = false;
            var numPoints = points.Length;
            for (var a = 0; a < numPoints; a++)
            {
                var b = (a + 1) % numPoints;
                var c = (b + 1) % numPoints;

                var crossProduct = CrossProductLength(points[a].X, points[a].Y, points[b].X, points[b].Y, points[c].X, points[c].Y);
                if (crossProduct < 0)
                {
                    gotNegative = true;
                }
                else if (crossProduct > 0)
                {
                    gotPositive = true;
                }
                if (gotNegative && gotPositive) return false;
            }
            return true;
        }

        public static float CrossProductLength(float ax, float ay, float bx, float @by, float cx, float cy)
        {
            // Get the vectors' coordinates.
            var bAx = ax - bx;
            var bAy = ay - @by;
            var bCx = cx - bx;
            var bCy = cy - @by;

            // Calculate the Z coordinate of the cross product.
            return (bAx * bCy - bAy * bCx);
        }

    }
}