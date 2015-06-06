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
using Newtonsoft.Json;
using System.IO;

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

            //using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            //{
            //    var libraryFile = "library.json";
            //    //does the library file exist?
            //    if (!isf.FileExists(libraryFile))
            //    {
            //        //load existing library
            //        using (var sw = new StreamWriter(isf.CreateFile(libraryFile)))
            //        {
                        
            //        }
            //    }

            //}

            //var data = JsonConvert.DeserializeObject<PlayListEntry[]>(sr.ReadToEnd());

            ////load the librart
            //foreach (var d in data)
            //{
            //    Collection.Add(d);
            //}


            //var musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            var sl = ShellLibrary.Load(KnownFolders.MusicLibrary, true);
            foreach (var l in sl)
            {
                Debug.WriteLine(l.Path);

                //setup filesystem watcher
            }
        }

        #endregion

    }
}
