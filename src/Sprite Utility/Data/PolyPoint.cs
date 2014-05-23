using System.Diagnostics;
using System.IO.Packaging;
using Boxer.Properties;
using Newtonsoft.Json;

namespace Boxer.Data
{
    [DebuggerDisplay("{X},{Y} ({MappedX}, {MappedY})")]
    public class PolyPoint : NodeWithName
    {
        private int _x;
        private int _y;

        [JsonProperty("x")]
        public int X { get { return _x; } set { Set(ref _x, value); } }

        [JsonProperty("y")]
        public int Y { get { return _y; } set { Set(ref _y, value); } }

        [JsonProperty("mapped_x")]
        public int MappedX
        {
            get
            {
                if (Settings.Default.TrimToMinimalNonTransparentArea)
                {
                    return X - (Parent.Parent.Parent as ImageFrame).TrimRectangle.X;
                }
                return X;
            }
        }

        [JsonProperty("mapped_y")]
        public int MappedY
        {
            get
            {
                if (Settings.Default.TrimToMinimalNonTransparentArea)
                {
                    return Y - (Parent.Parent.Parent as ImageFrame).TrimRectangle.Y;
                }
                return Y;
            }
        }

        public PolyPoint()
        { }

        public PolyPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static PolyPoint operator -(PolyPoint a, PolyPoint b)
        {
            return new PolyPoint(a.X - b.X, a.Y- b.Y);
        }

        public static PolyPoint operator +(PolyPoint a, PolyPoint b)
        {
            return new PolyPoint(a.X + b.X, a.Y + b.X);
        }

        public static PolyPoint operator / (PolyPoint a, PolyPoint b)
        {
            return new PolyPoint(a.X / b.X, a.Y/ b.Y);
        }

        public static PolyPoint operator /(PolyPoint a, int number)
        {
            return  new PolyPoint(a.X / number, a.Y/number);
        }

        public static PolyPoint Empty()
        {
            return  new PolyPoint(0,0);
        }
    }
}
