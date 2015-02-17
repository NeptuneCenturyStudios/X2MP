using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using X2MP.Core;

namespace X2MP.Models
{
    /// <summary>
    /// View model for Now Playing view
    /// </summary>
    class NowPlayingViewModel : BaseViewModel
    {
        #region Properties

        /// <summary>
        /// Gets the playlist
        /// </summary>
        public PlayList NowPlaying
        {
            get
            {
                return App.SoundEngine.NowPlaying;
            }
        }

        #region Commands

        #endregion

        #endregion

        #region Constructor
        public NowPlayingViewModel()
        {

        }
        #endregion

        #region Methods
        public void Drop(object sender, DragEventArgs e)
        {
            //check to see what the user dropped on top of us
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //get the files we have dropped
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                //filter out files that we don't support

                //tag reader
                using (var tagReader = new TagReader())
                {

                    //add files to the playlist
                    foreach (var file in files)
                    {

                        //read the tag
                        var tagInfo = tagReader.ReadTags(file);

                        //create playlist entry
                        var playlistEntry = new PlayListEntry() { TagInfo = tagInfo, FileName = file };

                        //add to now playing
                        App.SoundEngine.AddToNowPlaying(playlistEntry);

                    }
                }
            }
        }
        #endregion

        #region Register Commands
        private void RegisterCommands()
        {

        }
        #endregion

    }
}
