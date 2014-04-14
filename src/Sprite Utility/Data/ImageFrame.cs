using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Media;
using Boxer.Core;
using Boxer.Properties;
using Boxer.Services;
using Boxer.ViewModel;
using Boxer.Views;
using Newtonsoft.Json;

namespace Boxer.Data
{
    public sealed class ImageFrame : NodeWithName
    {
        private Rectangle _trimRectangle;

        [JsonProperty("trim_rectangle")]
        public Rectangle TrimRectangle
        {
            get { return _trimRectangle; }
            set { Set(ref _trimRectangle, value); }
        }

        private bool _isOpen;

        [JsonProperty("is_open")]
        public bool IsOpen
        {
            get { return _isOpen; }
            set { Set(ref _isOpen, value); }
        }


        private int _duration;

        [JsonProperty("duration")]
        public int Duration
        {
            get { return _duration; }
            set { Set(ref _duration, value); }
        }

         [JsonProperty("mapped_center_point_x")]
        public int MappedCenterPointX
        {
            get
            {
                if (Settings.Default.TrimToMinimalNonTransparentArea)
                {
                    return CenterPointX - TrimRectangle.X;
                }
                return CenterPointX;
            }
        }

        [JsonIgnore]
        public bool FailsAutoTrace { get; set; }

        [JsonProperty("mapped_center_point_y")]
        public int MappedCenterPointY
        {
            get
            {
                if (Settings.Default.TrimToMinimalNonTransparentArea)
                {
                    return CenterPointY - TrimRectangle.Y;
                }
                return CenterPointY;
            }
        }

        private int _centerPointX;
        private int _centerPointY;

        [JsonProperty("center_point_x")]
        public int CenterPointX { get { return _centerPointX; } set { Set(ref _centerPointX, value); } }

        [JsonProperty("center_point_y")]
        public int CenterPointY { get { return _centerPointY; } set { Set(ref _centerPointY, value); } }

        private byte[] _data;

        [JsonProperty("data")]
        public byte[] Data { get { return _data; } set { Set(ref _data, value); } }

        private int _width;

        [JsonProperty("width")]
        public int Width { get { return _width; } set { Set(ref _width, value); } }

        private int _height;

        [JsonProperty("height")]
        public int Height { get { return _height; } set { Set(ref _height, value); } }


        [JsonProperty("polygons")]
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

        private byte[] _thumbnail;

        [JsonProperty("thumbnail")]
        public byte[] Thumbnail { get { return _thumbnail; } set { Set(ref _thumbnail, value); } }

        private string _imagePath;

        [JsonIgnore]
        public ImageSource ThumbnailSource
        {
            get { return ImageHelper.ByteArrayToImageSource(Data); }
        }

        [JsonProperty("image_path")]
        public string ImagePath { get { return _imagePath; } set { Set(ref _imagePath, value); } }

        [JsonConstructor]
        public ImageFrame(IEnumerable<PolygonGroup> polygons)
            : this()
        {
            foreach (var polygonGroup in polygons)
            {
                AddChild(polygonGroup);
            }
        }

        public ImageFrame(byte[] data, int width, int height) : this(width, height)
        {
            Data = data;
            using (var imageData = new MemoryStream(Data))
            {
                TrimRectangle = BitmapTools.TrimRect(new Bitmap(imageData));
            }
        }

        public ImageFrame(int width, int height)
        {
            Data = null;
            Width = width;
            Height = height;
            Children = new FastObservableCollection<INode>();
            CenterPointX = CenterPointY = 0;
        }

        public ImageFrame()
        {
            Name = "Frame";
            Children = new FastObservableCollection<INode>();
        }

        [JsonIgnore]
        public SmartCommand<object> NewPolygonGroupCommand { get; private set; }

        public bool CanExecuteNewPolygonGroupCommand(object o)
        {
            return true;
        }

        public void ExecuteNewPolygonGroupCommand(object o)
        {
            var polygonGroup = new PolygonGroup();
            polygonGroup.Initialize();
            AddChild(polygonGroup);
        }

        [JsonIgnore]
        public SmartCommand<object> MarkOpenClosedStateCommand { get; private set; }

        public bool CanExecuteMarkOpenClosedStateCommand(object o)
        {
            return true;
        }

        public void ExecuteNewMarkOpenClosedStateCommand(object o)
        {
            IsOpen = !IsOpen;
            if (!Settings.Default.MarkAllAsOpen || !IsOpen) return;
            var index = Parent.Children.IndexOf(this);
            for (var i = index; i < Parent.Children.Count; i++)
            {
                var imageFrame = Parent.Children[i] as ImageFrame;
                if (imageFrame != null) imageFrame.IsOpen = true;
            }
        }

        [JsonIgnore]
        public SmartCommand<object> AutoTraceCommand { get; private set; }

        public bool CanExecuteAutoTraceCommand(object o)
        {
            return true;
        }

        public void ExecuteAutoTraceCommand(object o)
        {
            var dialog = new AutoTraceWindow();
            dialog.ShowDialog();

            var vm = dialog.DataContext as AutoTraceWindowVM;

            if (vm.IsOk)
            {
                using (var ms = new MemoryStream(Data))
                {
                    var imageBitmap = Image.FromStream(ms);
                    var errorBuilder = new StringBuilder();
                    var shape = TraceService.CreateComplexShape(imageBitmap, 20000, errorBuilder,
                        vm.HullTolerence,
                        vm.AlphaTolerence,
                        vm.MultipartDetection,
                        vm.HoleDetection);


                    if (shape != null)
                    {
                        var polygonGroup = new PolygonGroup("Body");
                        polygonGroup.Initialize();

                        var count = 1;
                        foreach (var polygon in shape.Vertices)
                        {
                            var poly = new Polygon() { Name = "Polygon " + count };

                            foreach (var point in polygon)
                            {
                                poly.AddChild(new PolyPoint((int)point.X, (int)point.Y));
                            }

                            polygonGroup.AddChild(poly);

                            count++;
                        }

                        AddChild(polygonGroup);
                    }

                    ms.Close();
                }
            }
        }

        protected override void InitializeCommands()
        {
            NewPolygonGroupCommand = new SmartCommand<object>(ExecuteNewPolygonGroupCommand, CanExecuteNewPolygonGroupCommand);
            MarkOpenClosedStateCommand = new SmartCommand<object>(ExecuteNewMarkOpenClosedStateCommand, CanExecuteMarkOpenClosedStateCommand);
            AutoTraceCommand = new SmartCommand<object>(ExecuteAutoTraceCommand, CanExecuteAutoTraceCommand);
            base.InitializeCommands();
        }
    }
}
