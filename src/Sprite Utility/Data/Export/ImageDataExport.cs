using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SpriteUtility;

namespace Boxer.Data.Export
{
    [Serializable]
    public class ImageDataExport
    {
        [JsonProperty("image_file")]
        public string ImageFile { get; set; }

        [JsonProperty("frames")]
        public List<ImageFrameExport> Frames { get; set; }

        public ImageDataExport(ImageData data)
        {
            ImageFile = data.Filename;
            Frames = new List<ImageFrameExport>(data.Children.Count);
            foreach (var frame in data.Children.Cast<ImageFrame>())
            {
                Frames.Add(new ImageFrameExport(frame));
            }
        }
    }
}