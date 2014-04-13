using System;
using System.Collections.Generic;
using System.Linq;
using Boxer.Data;
using Boxer.Data.Export;
using Newtonsoft.Json;

namespace SpriteUtility.Data.Export
{
    [Serializable]
    public class FolderExport
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("images", Required = Required.AllowNull)]
        public List<ImageDataExport> Images { get; set; }

        [JsonProperty("folders", Required = Required.AllowNull)]
        public List<FolderExport> Folders { get; set; }

        public FolderExport(Folder folder)
        {

            Name = folder.Name;

            var images = folder.Children.Where(p => p is ImageData).Cast<ImageData>();
            var folders = folder.Children.Where(p => p is Folder).Cast<Folder>();

            Images = new List<ImageDataExport>();

            foreach (var image in images)
            {
                Images.Add(new ImageDataExport(image));
            }

            Folders = new List<FolderExport>();

            foreach (var fold in folders)
            {
                Folders.Add(new FolderExport(fold));
            }
        }
    }
}