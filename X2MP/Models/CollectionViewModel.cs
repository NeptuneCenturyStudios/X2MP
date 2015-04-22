using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X2MP.Core;

namespace X2MP.Models
{
    class CollectionViewModel : BaseViewModel
    {

        public ObservableCollection<PlayListEntry> Collection { get; set; }
        public CollectionViewModel()
        {
            Collection = new ObservableCollection<PlayListEntry>();
        }

    }
}
