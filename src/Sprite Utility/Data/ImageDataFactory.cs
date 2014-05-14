using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Windows.Forms;
using Boxer.Services;
using FarseerPhysics;
using Settings = Boxer.Properties.Settings;

namespace Boxer.Data
{
    public class ImageDataFactory
    {
        public static void ImportFromExistingDirectoryDialog(Document document)
        {
            var dialog = new FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.Desktop,
                ShowNewFolderButton = false,
                SelectedPath = Settings.Default.LastFolderBrowsed
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.LastFolderBrowsed = dialog.SelectedPath;
                Settings.Default.Save();

                var rootFolder = ImportFromFolder(dialog.SelectedPath);
                document.AddChild(rootFolder);
            }

        }

        public static void ImportFromExistingDirectoryDialog(Folder folder)
        {
            var dialog = new FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.Desktop,
                ShowNewFolderButton = false,
                SelectedPath = Settings.Default.LastFolderBrowsed
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.LastFolderBrowsed = dialog.SelectedPath;
                Settings.Default.Save();

                var rootFolder = ImportFromFolder(dialog.SelectedPath);
                folder.AddChild(rootFolder);
            }
        }

        private static Folder ImportFromFolder(string selectedPath)
        {
            var rootDirectory = selectedPath;
            var rootFolder = new Folder();
            rootFolder.Initialize();
            rootFolder.Name = Path.GetFileNameWithoutExtension(rootDirectory);

            var directories = Directory.GetDirectories(rootDirectory);

            List<string> images;
            foreach (var directory in directories)
            {
                var folder = ImportFromFolder(directory);

                rootFolder.AddChild(folder);
            }

            images = Directory.GetFiles(rootDirectory, "*.png", SearchOption.TopDirectoryOnly).ToList();
            images.AddRange(Directory.GetFiles(rootDirectory, "*.gif", SearchOption.TopDirectoryOnly));

            foreach (var filename in images)
            {
                var imageData = CreateFromFilename(filename);
                rootFolder.AddChild(imageData);
            }
            return rootFolder;
        }

        public static ImageData CreateFromFilename(string filename)
        {
            var imageData = new ImageData(filename);
            imageData.Initialize();

            foreach (var frame in imageData.Children.Cast<ImageFrame>())
            {
                SetNaturalCenter(frame);
                EnsureDefaults(frame, false);
            }

            return imageData;
        }

        public static bool EnsureDefaults(ImageFrame frame, bool rebuildAll)
        {
            if (frame.TrimRectangle == Rectangle.Empty)
            {
                // There is no significant space in the cell, i.e., it's blank.
                //   We don't bother drawing any boxes for it because these would cause errors,
                //   though this can make sense for situations like "blinking"
                return true;
            }

            bool addAttack = true,
                 addClipping = true,
                 addPlatform = true,
                 addLanding = true,
                 addFoot = true,
                 addDepth = true,
                 addBody = true
                 ;

            var toRemove = new List<INode>();
            foreach (var child in frame.Children)
            {
                if (!(child is PolygonGroup)) continue;
                if (child.Name == "Attack")
                {
                    if (rebuildAll)
                    {
                        toRemove.Add(child);
                    }
                    else
                    {
                        addAttack = false;
                    }
                }
                if (child.Name == "Clipping")
                {
                    if (rebuildAll)
                    {
                        toRemove.Add(child);
                    }
                    else
                    {
                        addClipping = false;
                    }
                }
                if (child.Name == "Platform")
                {
                    if (rebuildAll)
                    {
                        toRemove.Add(child);
                    }
                    else
                    {
                        addPlatform = false;
                    }
                }
                if (child.Name == "Landing")
                {
                    if (rebuildAll)
                    {
                        toRemove.Add(child);
                    }
                    else
                    {
                        addLanding = false;
                    }
                }
                if (child.Name == "Foot")
                {
                    if (rebuildAll)
                    {
                        toRemove.Add(child);
                    }
                    else
                    {
                        addFoot = false;
                    }
                }
                if (child.Name == "Depth")
                {
                    if (rebuildAll)
                    {
                        toRemove.Add(child);
                    }
                    else
                    {
                        addDepth = false;
                    }
                }
                if (child.Name == "Body")
                {
                    if (rebuildAll)
                    {
                        toRemove.Add(child);
                    }
                    else
                    {
                        addBody = false;
                    }
                }
            }

            foreach (var child in toRemove)
            {
                frame.Children.Remove(child);
            }

            if(addAttack) AddAttackBoxStub(frame);
            if(addClipping) AddClippingBoxStub(frame);
            if(addPlatform) AddPlatformBoxStub(frame);
            if(addLanding) AddLandingBoxStub(frame);
            if(addFoot) AddDefaultFootBox(frame);
            if(addDepth) AddDefaultDepthBox(frame);
            if(addBody && !frame.FailsAutoTrace) AddBodyTrace(frame);

            return addAttack || addClipping || addPlatform || addFoot || addDepth || addBody;
        }

        private static void AddDefaultDepthBox(ImageFrame frame)
        {
            var depthGroup = new PolygonGroup { Name = "Depth", Parent = frame };
            depthGroup.Initialize();

            frame.AddChild(depthGroup);
            var depth = new Polygon { Name = "Depth", Parent = depthGroup };

            var bottom = frame.TrimRectangle.Bottom - 1;
            var left = frame.TrimRectangle.Left;
            var right = frame.TrimRectangle.Right;
            var width = frame.TrimRectangle.Width;

            var defaultDepthPercentage = (int)(frame.TrimRectangle.Height * 0.125f);
            const float defaultWidthBorder = 0.9f; // 10% on each side = 80%

            var tl = new PolyPoint(left + (int)(width * defaultWidthBorder), bottom - defaultDepthPercentage) { Parent = depth };
            var tr = new PolyPoint(right - (int)(width * defaultWidthBorder), bottom - defaultDepthPercentage) { Parent = depth };
            var br = new PolyPoint(right - (int)(width * defaultWidthBorder), bottom) { Parent = depth };
            var bl = new PolyPoint(left + (int)(width * defaultWidthBorder), bottom) { Parent = depth };
            depth.AddChild(tl);
            depth.AddChild(tr);
            depth.AddChild(br);
            depth.AddChild(bl);

            depthGroup.AddChild(depth);
        }

        private static void AddBodyTrace(ImageFrame frame)
        {
            using (var ms = new MemoryStream(frame.Data))
            {
                var imageBitmap = Image.FromStream(ms);
                var errorBuilder = new StringBuilder();
                var shape = TraceService.CreateSimpleShape(imageBitmap, 200, errorBuilder);

                if (shape == null)
                {
                    frame.FailsAutoTrace = true;
                    return;
                }

                var bodyGroup = new PolygonGroup { Name = "Body", Parent = frame };
                bodyGroup.Initialize();

                var count = 1;
                foreach (var polygon in shape.Vertices)
                {
                    var poly = new Polygon() { Name = "Polygon " + count, Parent = bodyGroup };


                    foreach (var point in polygon)
                    {
                        var x = (int)ConvertUnits.ToDisplayUnits(point.X);
                        var y = (int)ConvertUnits.ToDisplayUnits(point.Y);

                        x += (int)(frame.Width * 0.5f);
                        y += (int)(frame.Height * 0.5f);

                        poly.AddChild(new PolyPoint(x, y) { Parent = poly });
                    }

                    bodyGroup.AddChild(poly);
                    count++;
                }

                frame.AddChild(bodyGroup);
            }
        }

        private static void SetNaturalCenter(ImageFrame frame)
        {
            var left = frame.TrimRectangle.Left;
            var width = frame.TrimRectangle.Width;
            var height = frame.TrimRectangle.Height;
            var top = frame.TrimRectangle.Top;

            frame.CenterPointX = left + (int)(width * 0.5f);
            frame.CenterPointY = top + (int)(height * 0.5f);
        }

        private static void AddDefaultFootBox(ImageFrame frame)
        {
            var footGroup = new PolygonGroup { Name = "Foot", Parent = frame };
            footGroup.Initialize();

            frame.AddChild(footGroup);
            var foot = new Polygon { Name = "Foot", Parent = footGroup };

            var bottom = frame.TrimRectangle.Bottom - 1;
            var left = frame.TrimRectangle.Left;
            var right = frame.TrimRectangle.Right;
            var width = frame.TrimRectangle.Width;

            var tl = new PolyPoint(left + (int)(width * 0.25f), bottom - 2) { Parent = foot };
            var tr = new PolyPoint(right - (int)(width * 0.25f), bottom - 2) { Parent = foot };
            var br = new PolyPoint(right - (int)(width * 0.25f), bottom) { Parent = foot };
            var bl = new PolyPoint(left + (int)(width * 0.25f), bottom) { Parent = foot };

            foot.AddChild(tl);
            foot.AddChild(tr);
            foot.AddChild(br);
            foot.AddChild(bl);

            footGroup.AddChild(foot);
        }

        private static void AddPlatformBoxStub(ImageFrame frame)
        {
            var platformGroup = new PolygonGroup { Name = "Platform", Parent = frame };
            platformGroup.Initialize();

            frame.AddChild(platformGroup);
            var attack = new Polygon { Name = "Polygon 1", Parent = platformGroup };
            platformGroup.AddChild(attack);
        }

        private static void AddLandingBoxStub(ImageFrame frame)
        {
            var landingGroup = new PolygonGroup { Name = "Landing", Parent = frame };
            landingGroup.Initialize();

            frame.AddChild(landingGroup);
            var attack = new Polygon { Name = "Polygon 1", Parent = landingGroup};
            landingGroup.AddChild(attack);
        }

        private static void AddAttackBoxStub(ImageFrame frame)
        {
            var attackGroup = new PolygonGroup { Name = "Attack", Parent = frame};
            attackGroup.Initialize();

            frame.AddChild(attackGroup);
            var attack = new Polygon { Name = "Polygon 1", Parent = attackGroup };
            attackGroup.AddChild(attack);
        }

        private static void AddClippingBoxStub(ImageFrame frame)
        {
            var attackGroup = new PolygonGroup { Name = "Clipping", Parent = frame };
            attackGroup.Initialize();

            frame.AddChild(attackGroup);
            var attack = new Polygon { Name = "Polygon 1", Parent = attackGroup };
            attackGroup.AddChild(attack);
        }
    }
}
