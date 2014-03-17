using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Boxer.Core;
using Boxer.Data;
using Boxer.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtility;
using Cursor = System.Windows.Input.Cursor;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = Microsoft.Xna.Framework.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace Boxer.Controls
{
    /// <summary>
    /// Interaction logic for ImageViewer.xaml
    /// </summary>
    public partial class ImageViewer : UserControl
    {
        public ImageFrame ImageFrame
        {
            get { return (ImageFrame)this.GetValue(ImageFrameProperty); }
            set
            {
                this.SetValue(ImageFrameProperty, value);
            }
        }
        public static readonly DependencyProperty ImageFrameProperty = DependencyProperty.Register(
          "ImageFrame", typeof(ImageFrame), typeof(ImageViewer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ImageFrameChangedCallback));

        public Polygon Polygon
        {
            get { return (Polygon)this.GetValue(PolygonProperty); }
            set
            {
                this.SetValue(PolygonProperty, value);
            }
        }
        public static readonly DependencyProperty PolygonProperty = DependencyProperty.Register(
          "Polygon", typeof(Polygon), typeof(ImageViewer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PolygonChangedCallback));

        public PolygonGroup PolygonGroup
        {
            get { return (PolygonGroup)this.GetValue(PolygonGroupProperty); }
            set
            {
                this.SetValue(PolygonGroupProperty, value);
            }
        }
        public static readonly DependencyProperty PolygonGroupProperty = DependencyProperty.Register(
          "PolygonGroup", typeof(PolygonGroup), typeof(ImageViewer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PolygonGroupChangedCallback));

        public bool IsNormalMode
        {
            get { return (bool)this.GetValue(IsNormalModeProperty); }
            set
            {
                this.SetValue(IsNormalModeProperty, value);
            }
        }
        public static readonly DependencyProperty IsNormalModeProperty = DependencyProperty.Register(
          "IsNormalMode", typeof(bool), typeof(ImageViewer), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsNormalModeChangedCallback));

        public bool IsPolygonMode
        {
            get { return (bool)this.GetValue(IsPolygonModeProperty); }
            set
            {
                this.SetValue(IsPolygonModeProperty, value);
            }
        }
        public static readonly DependencyProperty IsPolygonModeProperty = DependencyProperty.Register(
          "IsPolygonMode", typeof(bool), typeof(ImageViewer), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsPolygonModeChangedCallback));

        private static void PolygonChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            (dependencyObject as ImageViewer).imageViewer.Polygon = dependencyPropertyChangedEventArgs.NewValue as Polygon;
        }

        private static void PolygonGroupChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
                (dependencyObject as ImageViewer).imageViewer.PolygonGroup = dependencyPropertyChangedEventArgs.NewValue as PolygonGroup;
        }

        private static void ImageFrameChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
                (dependencyObject as ImageViewer).imageViewer.Image = dependencyPropertyChangedEventArgs.NewValue as ImageFrame;
        }

        private static void IsNormalModeChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyPropertyChangedEventArgs.NewValue is bool)
            {
                var value = (bool) dependencyPropertyChangedEventArgs.NewValue;
                if (value)
                {
                    (dependencyObject as ImageViewer).imageViewer.Mode = Mode.Center;
                    (dependencyObject as ImageViewer).IsPolygonMode = false;

                }
            }
        }

        private static void IsPolygonModeChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyPropertyChangedEventArgs.NewValue is bool)
            {
                var value = (bool)dependencyPropertyChangedEventArgs.NewValue;
                if (value)
                {
                    (dependencyObject as ImageViewer).imageViewer.Mode = Mode.Polygon;
                    (dependencyObject as ImageViewer).IsNormalMode = false;
                }
            }
        }

        public ImageViewer()
        {
            InitializeComponent();
            imageViewer.ModeChanged += imageViewer_ModeChanged;
            Messenger.Default.Register<DoZoomMessage>(this, p => { imageViewer.DoZoom(p.HowMany); });
            Messenger.Default.Register<ResetZoomMessage>(this, p => { imageViewer.ResetZoom(); });
        }

        void imageViewer_ModeChanged(object sender, EventArgs e)
        {
            if (imageViewer.Mode == Mode.Center)
            {
                IsNormalMode = true;
            }
            if (imageViewer.Mode == Mode.Polygon)
            {
                IsPolygonMode = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            //paning with arrow keys
            if (e.Key == Key.Down)
                imageViewer.MoveCamera(Vector2.UnitY);
            if (e.Key == Key.Up)
                imageViewer.MoveCamera(-Vector2.UnitY);
            if (e.Key == Key.Right)
                imageViewer.MoveCamera(Vector2.UnitX);
            if (e.Key == Key.Left)
                imageViewer.MoveCamera(-Vector2.UnitX);

            e.Handled = true;

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            //delete polygon with delete key
            if (e.Key == Key.Delete)
            {
                if (Polygon != null)
                {
                    Polygon.Remove();
                }
                else if (PolygonGroup != null)
                {
                    PolygonGroup.Remove();
                }
            }

            //set center mode with c key
            if (e.Key == Key.C)
            {
                IsNormalMode = true;

            }
            //set polygon mode with p key
            if (e.Key == Key.P)
            {
                IsPolygonMode = true;
            }

             if (Keyboard.IsKeyDown(Key.LeftCtrl) && (e.Key == Key.Add))
                 imageViewer.DoZoom(1);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Subtract)
                imageViewer.DoZoom(-1);

            e.Handled = true;

            base.OnKeyUp(e);
        }
    }
}
