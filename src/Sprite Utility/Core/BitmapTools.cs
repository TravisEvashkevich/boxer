using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Boxer.Core
{
    public class BitmapTools
    {
        public static readonly Color DefaultTransparency = Color.FromArgb(255, 255, 0, 255);
        public static readonly Color PngTransparency = Color.FromArgb(0, 255, 255, 255);

        public static Rectangle TrimRect(Bitmap source, int minWidth = 0, int minHeight = 0)
        {
            // First replace any faux transparency with real for trimming purposes
            var replaced = ReplaceColor(source, DefaultTransparency, PngTransparency);
            replaced.Save("frim.png", ImageFormat.Png);

            Rectangle srcRect;
            BitmapData data = null;
            try
            {
                data = replaced.LockBits(new Rectangle(0, 0, replaced.Width, replaced.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var buffer = new byte[data.Height * data.Stride];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                int xMin = int.MaxValue,
                    xMax = int.MinValue,
                    yMin = int.MaxValue,
                    yMax = int.MinValue;

                var foundPixel = false;

                // Find xMin
                for (var x = 0; x < data.Width; x++)
                {
                    var stop = false;
                    for (var y = 0; y < data.Height; y++)
                    {
                        var alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha == 0) continue;
                        xMin = x;
                        stop = true;
                        foundPixel = true;
                        break;
                    }
                    if (stop)
                    {
                        break;
                    }
                }

                // Image is empty...
                if (!foundPixel)
                {
                    return Rectangle.Empty;
                }

                // Find yMin
                for (var y = 0; y < data.Height; y++)
                {
                    var stop = false;
                    for (var x = xMin; x < data.Width; x++)
                    {
                        var alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha == 0)
                        {
                            continue;
                        }
                        yMin = y;
                        stop = true;
                        break;
                    }
                    if (stop)
                    {
                        break;
                    }
                }

                // Find xMax
                for (var x = data.Width - 1; x >= xMin; x--)
                {
                    var stop = false;
                    for (var y = yMin; y < data.Height; y++)
                    {
                        var alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha == 0)
                        {
                            continue;
                        }
                        xMax = x;
                        stop = true;
                        break;
                    }
                    if (stop)
                    {
                        break;
                    }
                }

                // Find yMax
                for (var y = data.Height - 1; y >= yMin; y--)
                {
                    var stop = false;
                    for (var x = xMin; x <= xMax; x++)
                    {
                        var alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha == 0) continue;
                        yMax = y;
                        stop = true;
                        break;
                    }
                    if (stop)
                    {
                        break;
                    }
                }
                srcRect = Rectangle.FromLTRB(xMin, yMin, xMax + 1, yMax + 1);
            }
            finally
            {
                if (data != null)
                {
                    replaced.UnlockBits(data);
                }
            }

            return srcRect;
        }

        // http://stackoverflow.com/questions/17208254/how-to-change-pixel-color-of-an-image-in-c-net
        public static unsafe Bitmap ReplaceColor(Bitmap source,
                                  Color toReplace,
                                  Color replacement)
        {
            const int pixelSize = 4; // 32 bits per pixel

            Bitmap target = new Bitmap(
              source.Width,
              source.Height,
              PixelFormat.Format32bppArgb);

            BitmapData sourceData = null, targetData = null;

            try
            {
                sourceData = source.LockBits(
                  new Rectangle(0, 0, source.Width, source.Height),
                  ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                targetData = target.LockBits(
                  new Rectangle(0, 0, target.Width, target.Height),
                  ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                for (int y = 0; y < source.Height; ++y)
                {
                    byte* sourceRow = (byte*)sourceData.Scan0 + (y * sourceData.Stride);
                    byte* targetRow = (byte*)targetData.Scan0 + (y * targetData.Stride);

                    for (int x = 0; x < source.Width; ++x)
                    {
                        byte b = sourceRow[x * pixelSize + 0];
                        byte g = sourceRow[x * pixelSize + 1];
                        byte r = sourceRow[x * pixelSize + 2];
                        byte a = sourceRow[x * pixelSize + 3];

                        if (toReplace.R == r && toReplace.G == g && toReplace.B == b)
                        {
                            r = replacement.R;
                            g = replacement.G;
                            b = replacement.B;
                            a = replacement.A;
                        }

                        targetRow[x * pixelSize + 0] = b;
                        targetRow[x * pixelSize + 1] = g;
                        targetRow[x * pixelSize + 2] = r;
                        targetRow[x * pixelSize + 3] = a;
                    }
                }
            }
            finally
            {
                if (sourceData != null)
                    source.UnlockBits(sourceData);

                if (targetData != null)
                    target.UnlockBits(targetData);
            }

            return target;
        }
    }
}
