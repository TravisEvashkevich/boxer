using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using Boxer.Core;
using Newtonsoft.Json;
using Image = System.Drawing.Image;

namespace Boxer.Data
{
    public sealed class ImageData : NodeWithName
    {
        [JsonProperty("filename")]
        public string Filename
        {
            get
            {
                return Name + Extension;
            }
        }

        [JsonProperty("name")]
        public override string Name {
            get
            {
                return _name;
            }
            set
            {
                var extension = Path.GetExtension(value);
                var name = value;
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    Extension = extension;
                    name = Path.GetFileNameWithoutExtension(value);
                }

                Set(ref _name, name);
            }
        }

        private string _extension;

        [JsonProperty("extension")]
        public string Extension
        {
            get { return _extension; }
            set { Set(ref _extension, value); }
        }


        [JsonProperty("frames")]
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

        public ImageData()
        {
            Name = "New Image";
            Type = "Image";
            Children = new FastObservableCollection<INode>();
        }

        public ImageData(string filename)
            : this()
        {
            string extension;
            using (var stream = File.Open(filename, FileMode.Open))
            {
                using (var image = Image.FromStream(stream))
                {
                    extension = Path.GetExtension(filename);
                    var thumbnail = image.GetThumbnailImage(38, 38, null, new IntPtr());

                    if (extension != null && extension.Equals(".png"))
                    {
                        using (var ms = new MemoryStream())
                        {
                            image.Save(ms, ImageFormat.Png);

                            var frame = new ImageFrame(ms.ToArray(), image.Width, image.Height)
                            {
                                ImagePath = filename,
                                CenterPointX = image.Width / 2,
                                CenterPointY = image.Height / 2,
                                Thumbnail = ImageHelper.ImageToByteArray(thumbnail),
                                Name = "Frame 1",
                            };

                            frame.Initialize();
                            AddChild(frame);
                        }
                    }
                    else
                    {
                        extension = Path.GetExtension(filename);
                        if (extension != null && extension.Equals(".gif"))
                        {
                            var dimension = new FrameDimension(image.FrameDimensionsList[0]);

                            // Number of frames
                            var frameCount = image.GetFrameCount(dimension);
                            // Return an Image at a certain index

                            for (var i = 0; i < frameCount; i++)
                            {
                                image.SelectActiveFrame(dimension, i);

                                using (var ms = new MemoryStream())
                                {
                                    image.Save(ms, ImageFormat.Png);

                                    var child = new ImageFrame(ms.ToArray(), image.Width, image.Height);
                                    child.Initialize();
                                    AddChild(child);

                                    var frame = Children[i] as ImageFrame;
                                    if (frame == null) continue;

                                    frame.CenterPointX = image.Width / 2;
                                    frame.CenterPointY = image.Height / 2;
                                    frame.Thumbnail = ImageHelper.ImageToByteArray(thumbnail);
                                    frame.Name = "Frame " + (i + 1);
                                    var item = image.GetPropertyItem(0x5100); // FrameDelay in libgdiplus
                                    // Time is in 1/100th of a second, in miliseconds
                                    var time = (item.Value[0] + item.Value[1] * 256) * 10;
                                    frame.Duration = time;
                                }
                            }
                        }
                    }
                }
            }

            Name = Path.GetFileNameWithoutExtension(filename);
            Extension = extension;
        }
    }
}
