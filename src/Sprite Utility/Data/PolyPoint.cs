using Boxer.Properties;
using Newtonsoft.Json;

namespace Boxer.Data
{
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
    }
}
