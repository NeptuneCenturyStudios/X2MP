using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X2MP.Core
{
    public class NeedNextMediaEventArgs : EventArgs
    {
        public PlayListEntry Media { get; set; }
    }
}
