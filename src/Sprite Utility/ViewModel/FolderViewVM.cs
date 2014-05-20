using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Boxer.Core;
using Boxer.Data;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.ViewModel
{
    public class FolderViewVM : MainViewModel
    {
        private Folder _folder;

        public Folder Folder
        {
            get
            {
                return _folder;
            }
            set
            {
                Set(ref _folder, value);               
            }
        }



        protected override void InitializeCommands()
        {
         
        }
    }
}
