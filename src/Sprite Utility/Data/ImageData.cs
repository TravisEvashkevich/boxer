using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using Boxer.Core;
using Microsoft.Win32;
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
                base.Name = name;
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

        private DateTime _fileLastModified;
        [JsonProperty("fileLastModified")] 
        public DateTime FileLastModified 
        {
            get { return _fileLastModified; }
            set { Set(ref _fileLastModified, value); }
        }

        private string _filePath;

        [JsonProperty("filePath")]
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                Set(ref _filePath, value);
                Glue.Instance.DocumentIsSaved = false;
            }
        }

        public ImageData()
        {
            Name = "New Image";
            Type = "Image";
            Children = new FastObservableCollection<INode>();
        }

        public ImageData(string filename): this()
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
                                    // Replace indexed transparency (for use with spritesheets with indexed color)
                                    var img = BitmapTools.ReplaceIndexTransparency((Bitmap)image);
                                    img.Save(ms, ImageFormat.Png);

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
            FilePath = filename;
            FileLastModified = File.GetLastAccessTimeUtc(filename).ToUniversalTime();
            Name = Path.GetFileNameWithoutExtension(filename);
            Extension = extension;
        }

        #region Commands

        #region Reimport From New Path Command
        public SmartCommand<object> ReimportFromNewPathCommand { get; private set; }

        public void ExecuteReimportFromNewPathCommand(object o)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = string.Format("Find Image");
            dialog.Filter = ".png,.gif| *.png; *.gif";

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                //we'll check the filename they selected against the filename in the sound and ask if they want to overwrite
                //if they are different
                //get the path
                string fileName = Path.GetFileNameWithoutExtension(dialog.FileName);

                if (fileName != Name)
                {
                    if (MessageBox.Show(
                        String.Format("The selected file \"{0}\" has a different name than what you are trying to overwrite ({1}). Would you like to proceed anyway?", fileName, Name),
                        "FileName doesn't match", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        //pass the filename from the dialog to the Reimport new version so it will Create and overwrite
                        //the sound thus updating in the program.
                        ReimportImageData(dialog.FileName);
                        MessageBox.Show(String.Format("{0} was reimported successfully!", Path.GetFileNameWithoutExtension(fileName)), "Success!");
                    }
                }
                else
                {
                    ReimportImageData(dialog.FileName);
                    MessageBox.Show(String.Format("{0} was reimported successfully!", Path.GetFileNameWithoutExtension(fileName)), "Success!");
                }
                //make it so the user has to re approve to make sure that things are double checked.
                Approved = false;
            }
        }

        public void ReimportImageData(string fileName)
        {
            string extension;
            using (var stream = File.Open(fileName, FileMode.Open))
            {
                using (var image = Image.FromStream(stream))
                {
                    extension = Path.GetExtension(fileName);
                    var thumbnail = image.GetThumbnailImage(38, 38, null, new IntPtr());

                    if (extension != null && extension.Equals(".png"))
                    {
                        using (var ms = new MemoryStream())
                        {
                            image.Save(ms, ImageFormat.Png);

                            var frame = new ImageFrame(ms.ToArray(), image.Width, image.Height)
                            {
                                ImagePath = fileName,
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
                        extension = Path.GetExtension(fileName);
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
                                    // Replace indexed transparency (for use with spritesheets with indexed color)
                                    var img = BitmapTools.ReplaceIndexTransparency((Bitmap)image);
                                    img.Save(ms, ImageFormat.Png);
                                    
                                    var child = new ImageFrame(ms.ToArray(), image.Width, image.Height);
                                    //child.Initialize();
                                    //AddChild(child);
                                    if (frameCount == Children.Count)
                                    {
                                        (Children[i] as ImageFrame).Data = child.Data;
                                        var temp = image.GetThumbnailImage(38, 38, null, new IntPtr());
                                        //make sure to update the thumbnail.
                                        (Children[i] as ImageFrame).ThumbnailSource = ImageHelper.ByteArrayToImageSource((Children[i] as ImageFrame).Data);
                                        
                                    }
                                    else
                                    {
                                        //we'll have to figure out what to do if the new animation has more frames...
                                    }

                                    var frame = Children[i] as ImageFrame;
                                    if (frame == null) continue;

                                    frame.CenterPointX = image.Width / 2;
                                    frame.CenterPointY = image.Height / 2;
                                    //frame.Thumbnail = ImageHelper.ImageToByteArray(thumbnail);
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
            FilePath = fileName;
            //FileLastModified = File.GetLastAccessTimeUtc(filename).ToUniversalTime();
            Name = Path.GetFileNameWithoutExtension(fileName);
            Extension = extension;
        }

        #endregion


        protected override void InitializeCommands()
        {
            ReimportFromNewPathCommand = new SmartCommand<object>(ExecuteReimportFromNewPathCommand);
            base.InitializeCommands();
        }
        #endregion
    }
}
