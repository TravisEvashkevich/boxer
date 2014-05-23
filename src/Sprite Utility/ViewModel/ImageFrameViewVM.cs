using Boxer.Core;
using Boxer.Data;
using GalaSoft.MvvmLight.Messaging;

namespace Boxer.ViewModel
{
    public class DoZoomMessage
    {
        public int HowMany { get; set; }
    }

    public class ResetZoomMessage
    {
    }

    public class ImageFrameViewVM : MainViewModel
    {

        private bool _isNormalMode;

        public bool IsNormalMode
        {
            get { return _isNormalMode; }
            set
            {
                Set(ref _isNormalMode, value);
            }
        }

        private bool _isPolygonMode;

        public bool IsPolygonMode
        {
            get { return _isPolygonMode; }
            set
            {
                Set(ref _isPolygonMode, value);
            }
        }

        private bool _isMoveMode;

        public bool IsMoveMode
        {
            get { return _isMoveMode; }
            set
            {
                Set(ref _isMoveMode, value);
            }
        }

        private bool _showPolygonTextBox;

        public bool ShowPolygonTextBox
        {
            get { return _showPolygonTextBox; }
            set { Set(ref _showPolygonTextBox, value); }
        }

        private bool _showPolygonGroupTextBox;

        public bool ShowPolygonGroupTextBox
        {
            get { return _showPolygonGroupTextBox; }
            set { Set(ref _showPolygonGroupTextBox, value); }
        }

        private PolygonGroup _polygonGroup;

        public PolygonGroup PolygonGroup
        {
            get { return _polygonGroup; }
            set{ Set(ref _polygonGroup, value);}
        }

        private Polygon _polygon;

        public Polygon Polygon
        {
            get { return _polygon; }
            set { Set(ref _polygon, value); }
        }

        private ImageFrame _frame;

        public ImageFrame Frame
        {
            get { return _frame; }
            set { Set(ref _frame, value); }
        }

        public SmartCommand<object> ZoomInCommand { get; private set; }

        public bool CanExecuteZoomInCommand(object o)
        {
            return true;
        }

        public void ExecuteZoomInCommand(object o)
        {
            var message = new DoZoomMessage();
            message.HowMany = 1;

            Messenger.Default.Send(message);
        }

        public SmartCommand<object> ZoomOutCommand { get; private set; }

        public bool CanExecuteZoomOutCommand(object o)
        {
            return true;
        }

        public void ExecuteZoomOutCommand(object o)
        {
            var message = new DoZoomMessage();
            message.HowMany = -1;

            Messenger.Default.Send(message);
        }

        public SmartCommand<object> ResetZoomCommand { get; private set; }

        public bool CanExecuteResetZoomCommand(object o)
        {
            return true;
        }

        public void ExecuteResetZoomCommand(object o)
        {
            var message = new ResetZoomMessage();

            Messenger.Default.Send(message);
        }

        protected override void InitializeCommands()
        {
            ZoomInCommand = new SmartCommand<object>(ExecuteZoomInCommand, CanExecuteZoomInCommand);
            ZoomOutCommand = new SmartCommand<object>(ExecuteZoomOutCommand, CanExecuteZoomOutCommand);
            ResetZoomCommand = new SmartCommand<object>(ExecuteResetZoomCommand, CanExecuteResetZoomCommand);
        }
    }
}