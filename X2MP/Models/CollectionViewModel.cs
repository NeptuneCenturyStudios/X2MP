using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;
using X2MP.Core;
using Microsoft.WindowsAPICodePack.Shell;
using System.Diagnostics;

namespace X2MP.Models
{
    public class CollectionViewModel : BaseViewModel
    {
        #region Properties
        public ObservableCollection<PlayListEntry> Collection { get; set; }
        #endregion

        #region Constructor
        public CollectionViewModel()
        {
            Collection = new ObservableCollection<PlayListEntry>();

            //var musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            var sl = ShellLibrary.Load(KnownFolders.MusicLibrary, true);
            foreach (var l in sl)
            {
                Debug.WriteLine(l.Path);
            }
        }

        #endregion

    }
}
