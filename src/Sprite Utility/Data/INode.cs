using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;

namespace Boxer.Data
{
    public interface INode : IName
    {
        string Type { get; set; }

        ObservableCollection<INode> Children { get; set; }

        INode Parent { get; set; }
    }
}
