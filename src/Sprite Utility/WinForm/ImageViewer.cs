using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Boxer.Core;
using Boxer.Data;
using Boxer.Properties;
using Boxer.ViewModel;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WinFormsGraphicsDevice;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Boxer.WinForm
{
    public enum Mode
    {
        Center,
        Polygon,
        PolygonGroup
    }

    public class ImageViewer : GraphicsDeviceControl
    {
        #region Fields

        private static bool _paused;
        private static ImageViewer _instance;

        private readonly Cursor _arrowNormCursor;
        private readonly Cursor _arrowOverCursor;
        private readonly Cursor _penCursor;
        private readonly Cursor _penOverCursor;
        private Camera2D _camera2D;
        private Rectangle _centerRectangle;
        private Rectangle _currentCenterRectangle;
        private Rectangle _imageRectangle;
        private Rectangle _polygonRectangle;
        private Rectangle _trimRectangle;
        private bool _centerMoving;
        private int _currentX, _currentY;
        private ImageFrame _image;
        private Mode _mode;
        private PolyPoint _moving;
        private Polygon _poly;
        private PolygonGroup _polyGroup;
        private SpriteBatch _sprite;
        private Texture2D _texture;

        #endregion

        #region Events

        public event EventHandler<EventArgs> ModeChanged;

        protected void OnModeChanged(object sender, EventArgs e)
        {
            if (_mode == Mode.Center)
                Cursor = _arrowNormCursor;
            if (_mode == Mode.Polygon)
                Cursor = _penCursor;

            ModeChanged(sender, e);
        }

        #endregion

        #region Properties

        public Mode Mode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                OnModeChanged(this, EventArgs.Empty);
            }
        }

        public Polygon Polygon
        {
            get { return _poly; }
            set
            {
                _poly = value;
                Mode = Mode.Polygon;
            }
        }

        public PolygonGroup PolygonGroup
        {
            get { return _polyGroup; }
            set
            {
                _polyGroup = value;
            }
        }

        public static bool Paused
        {
            get { return _paused; }
            set
            {
                _paused = value;
                if (!_paused && _instance != null)
                    _instance.Invalidate();
            }
        }

        public ImageFrame Image
        {
            get { return _image; }
            set
            {
                _image = value;
                LoadImageToTexture();
            }
        }

        #endregion

        #region Constructor

        public ImageViewer()
        {
            _instance = this;

            _mode = Mode.Center;
            _paused = false;
            _poly = null;
            _polyGroup = null;
            _moving = null;
            if (!DesignMode)
            {
                _arrowNormCursor = new Cursor("Cursors/ArrowNorm.cur");
                _arrowOverCursor = new Cursor("Cursors/ArrowOver.cur");
                _penCursor = new Cursor("Cursors/Pen.cur");
                _penOverCursor = new Cursor("Cursors/PenOver.cur");
            }

            Timer refreshTimer = new Timer();
            refreshTimer.Interval = (int)(1000 * 1 / 30.0f);
            refreshTimer.Tick += Refresh;
            refreshTimer.Start();
        }

        #endregion

        #region Public Methods

        public void RedrawAfterDeletePolygon()
        {
            _poly = null;
            Draw();
        }

        public void ResetZoom()
        {
            _camera2D.ResetZoom();
        }

        public void MoveCamera(Vector2 vector)
        {
            _camera2D.Move(vector);
        }

        public void DoZoom(int howMany, Point? mouseLocation = null)
        {
            //if mouseLocation null get centerPoint coordinates in screen space
            if (mouseLocation == null)
            {
                Vector2 vector =
                    _camera2D.GetScreenCoordinates(new Vector2( _image.CenterPointX, _image.CenterPointY));

                mouseLocation = new Point((int) vector.X, (int) vector.Y);
            }

            _camera2D.DoZoom(howMany, mouseLocation);
        }

        #endregion

        #region Override Methods

        private Color ConvertDrawingColorToXNAColor(System.Windows.Media.Color color)
        {
            return new Color(color.R,
                color.G, color.B,
                color.A);
        }

        protected override void Initialize()
        {
            LoadImageToTexture();

            _sprite = new SpriteBatch(GraphicsDevice);

            _camera2D = new Camera2D(GraphicsDevice);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!_paused)
                base.OnPaint(e);
        }

        protected override void Draw()
        {
            GraphicsDevice.Clear(ConvertDrawingColorToXNAColor(Settings.Default.ViewerBackgroundColor));

            if (Image == null)
                return;
            _sprite.Begin(SpriteSortMode.Immediate,
                BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone,
                null,
                _camera2D.get_transformation());

            ////draw border
            if (Settings.Default.DrawBorder)
            {
                _sprite.DrawRectangle(_imageRectangle, ConvertDrawingColorToXNAColor(Settings.Default.BorderColor));
            }

           // draw image
            _sprite.Draw(_texture, new Vector2(0, 0), Color.White);

            if (Settings.Default.TrimToMinimalNonTransparentArea)
            {
                //draw trim rectangle
                _sprite.DrawRectangle(_trimRectangle, ConvertDrawingColorToXNAColor(Settings.Default.TrimBorderColor));
            }

            //draw non fixed center
            if (!Settings.Default.DrawLineArtForCenter)
            {
                DrawCenter(false);
            }

            _sprite.End();

            //start drawing with camera matrix
            _sprite.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);

            //draw fixed center
            if (Settings.Default.DrawLineArtForCenter)
            {
                DrawCenter(true);
            }

            //draw polygon group points
            if (_polyGroup != null)
            {
                Polygon selectedPoly = null;

                foreach (var polygon in _polyGroup.Children)
                {
                    if (_poly != null && _poly == polygon)
                    {
                        selectedPoly = polygon as Polygon;
                    }
                    else
                    {
                        foreach (PolyPoint p in polygon.Children)
                        {

                            Vector2 vector = _camera2D.GetScreenCoordinates(new Vector2(p.X, p.Y));
                            Vector2 centerVector = ShiftVectorToPixelCenter(vector);

                            int centerX = (int)centerVector.X - _polygonRectangle.Width / 2;
                            int centerY = (int)centerVector.Y - _polygonRectangle.Height / 2;
                            var polyRect = new Rectangle(centerX, centerY, _polygonRectangle.Width,
                                _polygonRectangle.Height);


                            _sprite.DrawRectangle(polyRect,
                                ConvertDrawingColorToXNAColor(Settings.Default.PolygonColor));
                        }
                    }
                }

                if (selectedPoly != null)
                {
                    foreach (PolyPoint p in selectedPoly.Children)
                    {

                        Vector2 vector = _camera2D.GetScreenCoordinates(new Vector2(p.X, p.Y));
                        Vector2 centerVector = ShiftVectorToPixelCenter(vector);

                        int centerX = (int)centerVector.X - _polygonRectangle.Width / 2;
                        int centerY = (int)centerVector.Y - _polygonRectangle.Height / 2;
                        var polyRect = new Rectangle(centerX, centerY, _polygonRectangle.Width,
                            _polygonRectangle.Height);


                        _sprite.DrawRectangle(polyRect,
                            ConvertDrawingColorToXNAColor(Settings.Default.PolygonSelectedColor));
                    }
                }

                //draw lines between points
                foreach (var polygon in _polyGroup.Children)
                {
                    if (polygon.Children.Count >= 2)
                    {
                        if (selectedPoly != polygon)
                        {
                            for (int i = 0; i < polygon.Children.Count; i++)
                            {
                                PolyPoint firstPoly = polygon.Children[i] as PolyPoint;

                                PolyPoint secondPoly;

                                if (i == polygon.Children.Count - 1)
                                {
                                    secondPoly = polygon.Children[0] as PolyPoint;
                                }
                                else
                                {
                                    secondPoly = polygon.Children[i + 1] as PolyPoint;
                                }

                                if (polygon.Children.Count == 2 && secondPoly == polygon.Children[0])
                                    continue;

                                Vector2 firstVector =
                                    _camera2D.GetScreenCoordinates(new Vector2(firstPoly.X, firstPoly.Y));
                                Vector2 secondVector =
                                    _camera2D.GetScreenCoordinates(new Vector2(secondPoly.X, secondPoly.Y));

                                Vector2 firstCenterVector = ShiftVectorToPixelCenter(firstVector);
                                Vector2 secondCenterVector = ShiftVectorToPixelCenter(secondVector);

                                _sprite.DrawLine(firstCenterVector, secondCenterVector,
                                    ConvertDrawingColorToXNAColor(Settings.Default.PolygonColor));
                            }
                        }
                    }
                }

                if (selectedPoly != null)
                {
                    if (selectedPoly.Children.Count >= 2)
                    {
                        for (int i = 0; i < selectedPoly.Children.Count; i++)
                        {
                            PolyPoint firstPoly = selectedPoly.Children[i] as PolyPoint;

                            PolyPoint secondPoly;

                            if (i == selectedPoly.Children.Count - 1)
                            {
                                secondPoly = selectedPoly.Children[0] as PolyPoint;
                            }
                            else
                            {
                                secondPoly = selectedPoly.Children[i + 1] as PolyPoint;
                            }

                            if (selectedPoly.Children.Count == 2 && secondPoly == selectedPoly.Children[0])
                                continue;

                            Vector2 firstVector =
                                _camera2D.GetScreenCoordinates(new Vector2(firstPoly.X, firstPoly.Y));
                            Vector2 secondVector =
                                _camera2D.GetScreenCoordinates(new Vector2(secondPoly.X, secondPoly.Y));

                            Vector2 firstCenterVector = ShiftVectorToPixelCenter(firstVector);
                            Vector2 secondCenterVector = ShiftVectorToPixelCenter(secondVector);

                            _sprite.DrawLine(firstCenterVector, secondCenterVector,
                                ConvertDrawingColorToXNAColor(Settings.Default.PolygonSelectedColor));
                        }
                    }
                }
            }

            _sprite.End();
        }

        protected override void Dispose(bool disposing)
        {
            if (_texture != null)
                _texture.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            //mouse wheel to control zoom with ctrl button
            if (ModifierKeys == Keys.Control)
            {
                if (e.Delta > 0)
                    DoZoom(1, new Point(e.X, e.Y));
                else if (e.Delta < 0)
                {
                    DoZoom(-1, new Point(e.X, e.Y));
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Focus();
            base.OnMouseEnter(e);
        }

        protected override void OnClick(EventArgs e)
        {
            Focus();
            base.OnClick(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            //if right down, get current mouse location
            if (e.Button == MouseButtons.Right)
            {
                _currentX = e.X;
                _currentY = e.Y;
            }

            //if left down
            if (e.Button == MouseButtons.Left)
            {
                //end center mode - move center point
                if (_mode == Mode.Center)
                {
                    _centerMoving = true;

                    Vector2 centerPoint = _camera2D.GetWorldCoordinates(new Vector2(e.X, e.Y));

                    _image.CenterPointX = (int)centerPoint.X;
                    _image.CenterPointY = (int)centerPoint.Y;
                }
                else if (_poly != null && _mode == Mode.Polygon && _moving == null)
                {
                    //check if selecting existing point
                    _moving = CheckIfMouseIsInPolygon(e.X, e.Y);

                    // if not add point
                    if (_moving == null && _poly.Children.Count < Settings.Default.MaxVerts)
                    {
                        var polyWorldCenter = _camera2D.GetWorldCoordinates(new Vector2(e.X, e.Y));
                        var p = new PolyPoint
                        {
                            X = (int) polyWorldCenter.X,
                            Y = (int) polyWorldCenter.Y,
                            Parent = _poly
                        };
                        _poly.Children.Add(p);
                        _moving = p;
                    }
                    else if( _poly.Children.Count >= Settings.Default.MaxVerts && _moving == null)
                    {
                        MessageBox.Show(
                            string.Format("Max Amount of Verts hit! The Max amount of verts for a polygon is {0}.",
                                Settings.Default.MaxVerts));
                    }
                }
            }
        }


        protected override void OnMouseUp(MouseEventArgs e)
        {
            // update CenterPoint
            if (_mode == Mode.Center && _centerMoving)
            {
                _centerMoving = false;
            }
            // update PolygonPoint
            else if (_mode == Mode.Polygon && _poly != null)
            {
                _moving = null;
                if (_poly == null || _poly.Children.Count <= 1) return;

                // Double check points for doubles on the same spot
                var removal = new List<int>();
                for (int i = 0; i < _poly.Children.Count; ++i)
                {
                    foreach (var node in _poly.Children)
                    {
                        var child = (PolyPoint) node;
                        var polyPoint = _poly.Children[i] as PolyPoint;
                        if (polyPoint == null || (child.X != polyPoint.X || child.Y != polyPoint.Y)) continue;

                        // Check to make sure that the point isn't actually the exact same one index wise etc.
                        if (child == _poly.Children[i]) continue;
                        if (!removal.Contains(i))
                        {
                            removal.Add(i);
                        }
                    }
                }
                
                if (removal.Count == 0) return;
                removal.Reverse();
                foreach (var i in removal)
                {
                    _poly.Children.RemoveAt(i);
                }
                MessageBox.Show(@"Removed duplicate vertices. Try not to do this... it breaks things.");
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            //if right button holded - pan
            if (e.Button == MouseButtons.Right)
            {
                var panVector = new Vector2((e.X - _currentX), (e.Y - _currentY));
                // panVector = _camera2D.GetWorldCoordinates(panVector);
                _camera2D.Move(-panVector);
               // _camera2D.Pos -= panVector;
                _currentX = e.X;
                _currentY = e.Y;
            }

            //if mode center and left button is holded - still move center
            if (_mode == Mode.Center && _centerMoving)
            {
                Vector2 centerPoint = _camera2D.GetWorldCoordinates(new Vector2(e.X, e.Y));

                _image.CenterPointX = (int)centerPoint.X;
                _image.CenterPointY = (int)centerPoint.Y;
            }
            //if mode polygon and left button is holded - still move polygon point
            else if (_mode == Mode.Polygon && _moving != null)
            {
                Vector2 polyWorldCenter = _camera2D.GetWorldCoordinates(new Vector2(e.X, e.Y));

                _moving.X = (int) polyWorldCenter.X;
                _moving.Y = (int) polyWorldCenter.Y;

                
            }

            //Indicate Cursor
            bool overCenter = false;
            bool overPoly = false;
            if ((_mode == Mode.Center) && CheckifMouseIsInCenter(e.X, e.Y))
            {
                overCenter = true;
            }
            else if (_poly != null && (_mode == Mode.Polygon))
            {
                PolyPoint polygon = CheckIfMouseIsInPolygon(e.X, e.Y);
                if (polygon != null)
                    overPoly = true;
            }

            if (_mode == Mode.Center)
            {
                if (overCenter)
                    Cursor = _arrowOverCursor;
                else
                {
                    Cursor = _arrowNormCursor;
                }
            }
            else
            {
                if (overPoly)
                    Cursor = _penOverCursor;
                else
                {
                    Cursor = _penCursor;
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            //if right button on polygon point - delete
            if (e.Button == MouseButtons.Right)
            {
                if (_poly != null && (_mode == Mode.Polygon))
                {
                    PolyPoint poly = CheckIfMouseIsInPolygon(e.X, e.Y);

                    if (poly != null)
                    {
                        _poly.Children.Remove(poly);
                        //set dirty
                        Glue.Instance.DocumentIsSaved = false;
                    }
                }
            }


            //if middle Mouse middle click add a new polygon and select it automatically for quickest way to keep making polys without extra movements etc.
            if (e.Button == MouseButtons.Middle)
            {
                var instance = ServiceLocator.Current.GetInstance<MainWindowVM>();
                instance.CreateNewPolygon();
                Glue.Instance.DocumentIsSaved = false;
            }
        }

        #endregion

        #region Private Methods

        //method which shifting polygons or center point (when are fixed size) to center of the pixel
        //sees on big zooms
        private Vector2 ShiftVectorToPixelCenter(Vector2 vector2)
        {
            Vector2 oneVector = _camera2D.GetScreenCoordinates(new Vector2(1, 1));
            Vector2 oneTwoVector = _camera2D.GetScreenCoordinates(new Vector2(1, 2));
            Vector2 difference = oneTwoVector - oneVector;
            float length = difference.Length();

            return vector2 + new Vector2(length, length)/2;
        }

        //check if mouse location is in polygon
        private PolyPoint CheckIfMouseIsInPolygon(int x, int y)
        {
            foreach (PolyPoint p in _poly.Children)
            {
                Vector2 vector = _camera2D.GetScreenCoordinates(new Vector2(p.X, p.Y));

                Vector2 centerVector = ShiftVectorToPixelCenter(vector);
                //we have to resize rectangle by 1, because strange drawing behaviour of Primitives2D lib
                var polygonRectangle = new Rectangle((int) centerVector.X - _polygonRectangle.Width/2,
                    (int) centerVector.Y - _polygonRectangle.Height/2, _polygonRectangle.Width + 1,
                    _polygonRectangle.Height + 1);

                if (polygonRectangle.Contains(new Point(x, y)))
                {
                    return p;
                }
            }

            return null;
        }

        //check if mouse location is in center
        private bool CheckifMouseIsInCenter(int x, int y)
        {
            Vector2 worldCursor = _camera2D.GetWorldCoordinates(new Vector2(x, y));
            //we have to resize rectangle by 1, because strange drawing behaviour of Primitives2D lib
            var centerRectangle = new Rectangle(_currentCenterRectangle.X, _currentCenterRectangle.Y,
                _currentCenterRectangle.Width + 1, _currentCenterRectangle.Height + 1);
            if (centerRectangle.Contains(new Point((int) worldCursor.X, (int) worldCursor.Y)))
            {
                return true;
            }

            return false;
        }

       // drawing center
        private void DrawCenter(bool isFixed)
        {
            int centerX, centerY;
            int x0 = 0;
            int y0 = 0;
            int xWidth = _image.Width;
            int yHeight = _image.Height;

            //if not fixed
            if (!isFixed)
            {
                _currentCenterRectangle = new Rectangle( _image.CenterPointX - _centerRectangle.Width/2,
                    (int) _image.CenterPointY - _centerRectangle.Height/2, _centerRectangle.Width,
                    _centerRectangle.Height);
                centerY = _currentCenterRectangle.Y + _currentCenterRectangle.Height/2;
                //have to add + 1 - strange behaviour of Primitive2D
                centerX = _currentCenterRectangle.X + _currentCenterRectangle.Width/2 + 1;
            }
            //otherwise
            else
            {
                //get center point in screen coordinates
                Vector2 vector =
                    _camera2D.GetScreenCoordinates(new Vector2(_image.CenterPointX, _image.CenterPointY));

                //get zero point in screen coordinates
                Vector2 vectorZero = _camera2D.GetScreenCoordinates(Vector2.Zero);

                //get max point (right-bottom corner) in screen coordinates
                Vector2 vectorMax = _camera2D.GetScreenCoordinates(new Vector2(xWidth, yHeight));

                //shift center point to center of the pixel
                vector = ShiftVectorToPixelCenter(vector);

                //coordinates for drawing center lines
                x0 = (int) vectorZero.X;
                y0 = (int) vectorZero.Y;
                xWidth = (int) vectorMax.X;
                yHeight = (int) vectorMax.Y;

                _currentCenterRectangle = new Rectangle((int) vector.X - _centerRectangle.Width/2,
                    (int) vector.Y - _centerRectangle.Height/2, _centerRectangle.Width, _centerRectangle.Height);
                centerY = _currentCenterRectangle.Y + _currentCenterRectangle.Height/2;
                //have to add + 1 - strange behaviour of Primitive2D
                centerX = _currentCenterRectangle.X + _currentCenterRectangle.Width/2 + 1;
            }

            //draw center line
            _sprite.DrawLine(new Vector2(x0, centerY), new Vector2(xWidth, centerY),
                ConvertDrawingColorToXNAColor(Settings.Default.CenterLineColor));
            _sprite.DrawLine(new Vector2(centerX, y0), new Vector2(centerX, yHeight),
                ConvertDrawingColorToXNAColor(Settings.Default.CenterLineColor));

            //draw center rectangle
            _sprite.DrawRectangle(_currentCenterRectangle, ConvertDrawingColorToXNAColor(Settings.Default.CenterPointColor), 1);

            //draw center cross
            _sprite.DrawLine(new Vector2(_currentCenterRectangle.Left, centerY),
                new Vector2(_currentCenterRectangle.Right, centerY), ConvertDrawingColorToXNAColor(Settings.Default.CenterPointColor));
            _sprite.DrawLine(new Vector2(centerX, _currentCenterRectangle.Top),
                new Vector2(centerX, _currentCenterRectangle.Bottom), ConvertDrawingColorToXNAColor(Settings.Default.CenterPointColor));
        }

        //load image to texture, rest neccesary fields
        private void LoadImageToTexture()
        {
            if (Image != null && GraphicsDevice != null)
            {
                _trimRectangle = new Rectangle(_image.TrimRectangle.X - 1, _image.TrimRectangle.Y - 1,
                    _image.TrimRectangle.Width + 1, _image.TrimRectangle.Height + 1);

                MemoryStream imageData;
                imageData = new MemoryStream(_image.Data);
                _texture = Texture2D.FromStream(GraphicsDevice, imageData);
                imageData.Close();

                _centerRectangle = new Rectangle(0, 0, 16, 16);
                _polygonRectangle = new Rectangle(0, 0, 8, 8);
                _imageRectangle = new Rectangle(-1, -1, _image.Width + 1, _image.Height + 1);
            }
        }

        private void Refresh(object sender, EventArgs e)
        {
            Invalidate();
        }

        #endregion
    }
}