using System;
using System.Windows;
using System.Windows.Input;
using Boxer.Data;
using Boxer.ViewModel;
using Boxer.WinForm;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
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

        public bool IsMovingMode
        {
            get { return (bool)this.GetValue(IsMovingModeProperty); }
            set
            {
                this.SetValue(IsMovingModeProperty, value);
            }
        }
        public static readonly DependencyProperty IsMovingModeProperty = DependencyProperty.Register(
        "IsMovingMode", typeof(bool), typeof(ImageViewer), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsMovingModeChangedCallback));

        public bool MoveAnyPolygon
        {
            get { return (bool)this.GetValue(MoveAnyPolygonProperty); }
            set
            {
                this.SetValue(MoveAnyPolygonProperty, value);
            }
        }
        public static readonly DependencyProperty MoveAnyPolygonProperty = DependencyProperty.Register(
        "MoveAnyPolygon", typeof(bool), typeof(ImageViewer), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, MoveAnyPolygonChangeCallback));



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
                    (dependencyObject as ImageViewer).IsMovingMode = false;
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
                    (dependencyObject as ImageViewer).IsMovingMode = false;
                }
            }
        }

        private static void IsMovingModeChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyPropertyChangedEventArgs.NewValue is bool)
            {
                var value = (bool)dependencyPropertyChangedEventArgs.NewValue;
                if (value)
                {
                    (dependencyObject as ImageViewer).imageViewer.Mode = Mode.Moving;
                    (dependencyObject as ImageViewer).IsNormalMode = false;
                    (dependencyObject as ImageViewer).IsPolygonMode = false;
                }
            }
        }

        private static void MoveAnyPolygonChangeCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyPropertyChangedEventArgs.NewValue is bool)
            {
                var value = (bool)dependencyPropertyChangedEventArgs.NewValue;
                (dependencyObject as ImageViewer).imageViewer.CanMoveAnyPolygon = value;
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
            if (imageViewer.Mode == Mode.Moving)
            {
                IsMovingMode = true;
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

            //----------Hotkey support---------
            //for users while in the imageviewer View (since apparently hotkeys in the mainwindow that usually work across the whole
            //application, don't get down to the other view)
            //TODO Kind of a hack, see if able to do another way
            //(already tried doing in the same way as mainwindow but in the ImageFrameView.xaml.cs, didn't work
            
            //Save As Command call 
            //NOTE: THIS HAS TO BE BEFORE SAVE, ELSE SAVE ONLY GETS CALLED
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && (e.Key == Key.S))
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.ExecuteSaveAsCommand(instance);
            }
            //Save Command call
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && (e.Key == Key.S))
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.ExecuteSaveDocumentCommand(instance);
            }
            
            //Export Command call
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && (e.Key == Key.E))
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.ExecuteExportCommand(instance);
            }
            //Open Command call
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && (e.Key == Key.O))
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.ExecuteOpenDocumentCommand(instance);
            }
            //New Command call
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && (e.Key == Key.N))
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.ExecuteNewDocumentCommand(instance);
            }
            //Close Command call
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && (e.Key == Key.Q))
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.ExecuteCloseCommand(instance);
            }
            //apparently do need these hot keys 
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ) && (e.Key == Key.Enter))
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.JumpBackOneImageFrame();
            }
            else if (e.Key == Key.Enter)
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.JumpToNextImageFrame();
            }
            
            
            

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
            if (e.Key == Key.M && Keyboard.IsKeyDown(Key.LeftShift))
            {
                //flip the mode
                MoveAnyPolygon = !MoveAnyPolygon;
            }
            else if (e.Key == Key.M)
            {
                IsMovingMode = true;
            }

             if (Keyboard.IsKeyDown(Key.LeftCtrl) && (e.Key == Key.Add))
                 imageViewer.DoZoom(1);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Subtract)
                imageViewer.DoZoom(-1);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.R)
                imageViewer.ResetZoom();
            e.Handled = true;


            base.OnKeyUp(e);
        }
    }
}
