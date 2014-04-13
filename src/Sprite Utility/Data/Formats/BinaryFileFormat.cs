using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
            var writer = new BinaryWriter(stream);
            WriteDocument(writer, document);
        }

        public override Document Load(string path)
        {
            var stream = File.OpenRead(path);
            var reader = new BinaryReader(stream);
            var document = new Document();
            ReadDocument(reader, document);

            return document;
        }

        private static void ReadDocument(BinaryReader reader, Document document)
        {
            document.Name = reader.ReadString();
            document.Filename = reader.ReadString();
            var count = reader.ReadInt32();
            ReadChildren(reader, document.Children, count);
        }

        private static void WriteDocument(BinaryWriter writer, Document document)
        {
            writer.Write(document.Name);
            writer.Write(document.Filename);
            writer.Write(document.Children.Count);
            WriteChildren(writer, document.Children);
        }
        
        private static void ReadChildren(BinaryReader reader, ICollection<INode> children, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var type = (Type)reader.ReadSByte();
                if (type == Type.Folder)
                {
                    ReadFolder(reader, children, reader.ReadInt32());
                }
                else
                {
                    ReadImageData(reader, children);
                }
            }
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

        private static void ReadFolder(BinaryReader reader, ICollection<INode> container, int count)
        {
            var folder = new Folder {Name = reader.ReadString()};
            container.Add(folder);
            ReadChildren(reader, folder.Children, count);
        }

        private static void WriteFolder(BinaryWriter writer, INode child)
        {
            writer.Write((sbyte) Type.Folder);
            writer.Write(child.Name);
            writer.Write(child.Children.Count);
            WriteChildren(writer, child.Children);
        }

        private static void ReadImageData(BinaryReader reader, ICollection<INode> container)
        {
            var name = reader.ReadString();
            var filename = reader.ReadString();
            var extension = reader.ReadString();
            var imageData = new ImageData(filename) { Name = name, Extension = extension };
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
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
                var frame = new ImageFrame(data, width, height);
                frame.Duration = duration;
                frame.ImagePath = imagePath;
                var thumbnailLength = reader.ReadInt64();
                var thumbnail = new byte[thumbnailLength];
                for (var j = 0; j < thumbnailLength; j++)
                {
                    thumbnail[j] = reader.ReadByte();
                }
                frame.CenterPointX = reader.ReadInt32();
                frame.CenterPointY = reader.ReadInt32();

                ReadPolygons(reader, frame.Children);
            }
            container.Add(imageData);
        }

        private static void WriteImageData(BinaryWriter writer, INode child)
        {
            var imageData = (ImageData) child;
            writer.Write((sbyte) Type.Images);
            writer.Write(imageData.Name);
            writer.Write(imageData.Filename);
            writer.Write(imageData.Extension);

            var frameCount = imageData.Children;
            writer.Write(frameCount.Count);
            foreach (var frame in imageData.Children.Cast<ImageFrame>())
            {
                writer.Write(frame.Width);
                writer.Write(frame.Height);
                writer.Write(frame.Duration);
                writer.Write(frame.ImagePath ?? imageData.Filename);
                writer.Write(frame.Data.LongLength);
                writer.Write(frame.Data);
                writer.Write(frame.Thumbnail.LongLength);
                writer.Write(frame.Thumbnail);
                writer.Write(frame.CenterPointX);
                writer.Write(frame.CenterPointY);

                WritePolygons(writer, frame);
            }
        }

        private static void ReadPolygons(BinaryReader reader, ICollection<INode> container)
        {
            var groupCount = reader.ReadInt32();
            for (var i = 0; i < groupCount; i++)
            {
                var polyGroup = new PolygonGroup();
                polyGroup.Name = reader.ReadString();
                var polyCount = reader.ReadInt32();
                for (var j = 0; j < polyCount; j++)
                {
                    var polygon = new Polygon();
                    polygon.Name = reader.ReadString();
                    var pointCount = reader.ReadInt32();
                    for (var k = 0; k < pointCount; k++)
                    {
                        var point = new PolyPoint(reader.ReadInt32(), reader.ReadInt32());
                        polygon.Children.Add(point);
                    }
                    polyGroup.Children.Add(polygon);
                }
                container.Add(polyGroup);
            }
        }

        private static void WritePolygons(BinaryWriter writer, INode parent)
        {
            var polyGroup = parent.Children.Cast<PolygonGroup>();
            var groupCount = parent.Children.Count();
            writer.Write(groupCount);
            foreach (var group in polyGroup)
            {
                writer.Write(@group.Name);
                writer.Write(@group.Children.Count);
                foreach (var polygon in @group.Children.Cast<Polygon>())
                {
                    writer.Write(polygon.Name);
                    writer.Write(polygon.Children.Count);
                    foreach (var point in polygon.Children.Cast<PolyPoint>())
                    {
                        writer.Write(point.X);
                        writer.Write(point.Y);
                    }
                }
            }
        }
    }
}