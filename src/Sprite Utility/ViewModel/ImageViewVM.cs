using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Boxer.Core;
using Boxer.Data;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.ViewModel
{
    public class ImageViewVM : MainViewModel
    {
        private ImageData _image;

        public ImageData Image
        {
            get { return _image; }
            set { Set(ref _image, value); }
        }

 
        protected override void InitializeCommands()
        {
        }
    }
}
