using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using Boxer.Properties;
using Boxer.ViewModel;
using Boxer.Views;
using Microsoft.Xna.Framework;
using Rectangle = System.Drawing.Rectangle;

namespace Boxer.Data.Formats
{
    public class BinaryFileFormat : FileFormat
    {
        public enum Type : sbyte
        {
            Folder = 1,
            Images = 2
        }

        public override void Save(string path, Document document)
        {
            using (var stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite))
            {
                var writer = new BinaryWriter(stream);
                WriteDocument(writer, document);
                stream.Flush();
            }
        }

        public override Document Load(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var reader = new BinaryReader(stream);
                var document = new Document();
                ReadDocument(reader, document);

                InitializeCommandTree(document);

                return document;
            }
        }

        public static void InitializeCommandTree(NodeWithName document)
        {
            // Initialize commands
            document.Initialize();
            var elements = Flatten(document.Children);
            Parallel.ForEach(elements, e => e.Initialize());
        }

        static IEnumerable<INode> Flatten(IEnumerable<INode> collection)
        {
            foreach (var node in collection)
            {
                yield return node;
                if (node.Children == null) continue;
                foreach (var child in Flatten(node.Children))
                {
                    yield return child;
                }
            }
        }

        private static void ReadDocument(BinaryReader reader, Document document)
        {
            document.Name = reader.ReadString();
            document.Filename = reader.ReadString();
            ReadChildren(reader, document);
        }

        private static void ReadChildren(BinaryReader reader, INode parent)
        {
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                ReadChild(reader, parent, parent.Children);
            }
        }

        private static void ReadChild(BinaryReader reader, INode parent, ICollection<INode> container)
        {
            var type = (Type)reader.ReadSByte();
            if (type == Type.Folder)
            {
                ReadFolder(reader, parent, container);
            }
            else
            {
                ReadImageData(reader, parent, container);
            }
        }

        private static void ReadFolder(BinaryReader reader, INode parent, ICollection<INode> container)
        {
            var folder = new Folder {Name = reader.ReadString(), Parent = parent};
            container.Add(folder);
            ReadChildren(reader, folder);
        }

        private static void WriteDocument(BinaryWriter writer, Document document)
        {
            writer.Write(document.Name);
            writer.Write(document.Filename);
            writer.Write(document.Children.Count);
            WriteChildren(writer, document.Children);
        }

        private static void WriteChildren(BinaryWriter writer, IEnumerable<INode> children)
        {
            foreach (var child in children)
            {
                if (child is Folder)
                {
                    WriteFolder(writer, child);
                }
                else
                {
                    WriteImageData(writer, child);
                }
            }
        }

        private static void WriteFolder(BinaryWriter writer, INode child)
        {
            writer.Write((sbyte)Type.Folder);
            writer.Write(child.Name);
            writer.Write(child.Children.Count);
            WriteChildren(writer, child.Children);
        }

        private static void ReadImageData(BinaryReader reader, INode parent, ICollection<INode> container)
        {
            var name = reader.ReadString();
            var extension = reader.ReadString();
            var approved = reader.ReadBoolean();
            //create the image and set it's data
            var imageData = new ImageData
            {
                Name = name, 
                Extension = extension,
                Parent = parent
            };
            imageData.Approved = approved;
            imageData.Type = "Image";
            //read in the frames of the image
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                ReadImageFrame(reader, imageData);
            }
            container.Add(imageData);
        }

        private static void ReadImageFrame(BinaryReader reader, INode imageData)
        {
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var duration = reader.ReadInt32();
            var imagePath = reader.ReadString();
            var dataLength = reader.ReadInt64();
            var data = new byte[dataLength];
            for (var j = 0; j < dataLength; j++)
            {
                data[j] = reader.ReadByte();
            }
            var frame = new ImageFrame(data, width, height)
            {
                Duration = duration,
                ImagePath = imagePath
            };
            var thumbnailLength = reader.ReadInt64();
            var thumbnail = new byte[thumbnailLength];
            for (var j = 0; j < thumbnailLength; j++)
            {
                thumbnail[j] = reader.ReadByte();
            }
            frame.CenterPointX = reader.ReadInt32();
            frame.CenterPointY = reader.ReadInt32();
            frame.IsOpen = reader.ReadBoolean();
            frame.FailsAutoTrace = reader.ReadBoolean();
            reader.ReadInt32(); // MappedCenterPointX
            reader.ReadInt32(); // MappedCenterPointY
            frame.TrimRectangle = new Rectangle(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32()
            );
            frame.Type = "ImageFrame";
            frame.Name = Path.GetFileNameWithoutExtension(imagePath);

            ReadPolygons(reader, frame, frame.Children);
            frame.Parent = imageData;
            imageData.Children.Add(frame);
        }

        private static void WriteImageData(BinaryWriter writer, INode child)
        {
            var imageData = (ImageData)child;
            writer.Write((sbyte)Type.Images);
            writer.Write(imageData.Name);
            writer.Write(imageData.Extension);
            writer.Write(imageData.Approved);

            var frameCount = imageData.Children;
            writer.Write(frameCount.Count);
            foreach (var frame in imageData.Children.Cast<ImageFrame>())
            {
                WriteImageFrame(writer, frame, imageData);
            }
        }

        private static void WriteImageFrame(BinaryWriter writer, ImageFrame frame, ImageData imageData)
        {
            writer.Write(frame.Width);
            writer.Write(frame.Height);
            writer.Write(frame.Duration);
            writer.Write(frame.ImagePath ?? imageData.Filename);
            writer.Write(frame.Data.LongLength);
            writer.Write(frame.Data);
            writer.Write(frame.Thumbnail != null? frame.Thumbnail.LongLength : 0);
            writer.Write(frame.Thumbnail ?? new byte[0]);
            writer.Write(frame.CenterPointX);
            writer.Write(frame.CenterPointY);
            writer.Write(frame.IsOpen);
            writer.Write(frame.FailsAutoTrace);
            writer.Write(frame.MappedCenterPointX);
            writer.Write(frame.MappedCenterPointY);
            writer.Write(frame.TrimRectangle.X);
            writer.Write(frame.TrimRectangle.Y);
            writer.Write(frame.TrimRectangle.Width);
            writer.Write(frame.TrimRectangle.Height);

            WritePolygons(writer, frame);
        }

        private static void ReadPolygons(BinaryReader reader, INode parent, ICollection<INode> container)
        {
            var groupCount = reader.ReadInt32();
            for (var i = 0; i < groupCount; i++)
            {
                var polyGroup = new PolygonGroup {Name = reader.ReadString(), Parent = parent};
                var polyCount = reader.ReadInt32();
                polyGroup.Type = "PolygonGroup";

                for (var j = 0; j < polyCount; j++)
                {
                    var polygon = new Polygon {Name = reader.ReadString(), Parent = polyGroup};
                    var pointCount = reader.ReadInt32();
                    polygon.Type = "Polygon";
                    for (var k = 0; k < pointCount; k++)
                    {
                        var point = new PolyPoint(reader.ReadInt32(), reader.ReadInt32()) {Parent = polygon};
                        reader.ReadInt32(); // MappedX
                        reader.ReadInt32(); // MappedY
                        point.Type = "PolyPoint";
                        polygon.Children.Add(point);
                    }
                    if (polygon.Children.Count > Settings.Default.MaxVerts)
                    {
                        Debug.Assert(false, string.Format("{0} has more than {1} vets in group {2} in {3} image", polygon.Name,Settings.Default.MaxVerts, polyGroup.Name, parent.Name));
                    }
                    polyGroup.Children.Add(polygon);
                }
                container.Add(polyGroup);
            }
        }

        private static void WritePolygons(BinaryWriter writer, ImageFrame parent)
        {
            var polyGroups = parent.Children.Cast<PolygonGroup>();
            var groupCount = parent.Children.Count();
            writer.Write(groupCount);
            foreach (var polyGroup in polyGroups)
            {
                writer.Write(polyGroup.Name);
                writer.Write(polyGroup.Children.Count);
                foreach (var polygon in polyGroup.Children.Cast<Polygon>())
                {
                    writer.Write(polygon.Name);
                    writer.Write(polygon.Children.Count);

                    var validPoints = new List<PolyPoint>();
                    foreach (var point in polygon.Children.Cast<PolyPoint>())
                    {
                        if (point.X < 0 || point.Y < 0)
                        {
                            Console.WriteLine(@"neg vertices for {0}!", parent.Name);
                            validPoints.Add(new PolyPoint(0, 0) { Parent = point.Parent});
                            continue;
                        }
                        validPoints.Add(point);
                    }

                    foreach (var point in validPoints)
                    {
                        writer.Write(point.X);
                        writer.Write(point.Y);
                        writer.Write(point.MappedX);
                        writer.Write(point.MappedY);
                    }
                }
            }
        }
    }
}