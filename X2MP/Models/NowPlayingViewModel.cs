using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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

        /// <summary>
        /// Gets or sets the selected playlist item
        /// </summary>
        private PlayListEntry _selectedItem;
        public PlayListEntry SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;

                //fire changed event
                OnPropertyChanged("SelectedItem");
                //affects commands
                DeleteItem.OnCanExecuteChanged();
            }
        }

        #region Commands
        /// <summary>
        /// Gets the command for the now playing button
        /// </summary>
        public Command DeleteItem { get; private set; }

        /// <summary>
        /// Clear the now playing list
        /// </summary>
        public Command ClearNowPlaying { get; private set; }
        #endregion

        #endregion

        #region Constructor
        public NowPlayingViewModel()
        {
            RegisterCommands();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Adds an array of files to the list
        /// </summary>
        /// <param name="files"></param>
        private void AddFile(TagReader tagReader, string file)
        {

            //read the tag
            var tagInfo = tagReader.ReadTags(file);

            if (tagInfo != null)
            {
                //create playlist entry
                var playlistEntry = new PlayListEntry() { TagInfo = tagInfo, FileName = file };

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    //add to now playing
                    App.SoundEngine.AddToNowPlaying(playlistEntry);
                });


            }
        }

        /// <summary>
        /// Handles dropping files onto the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Drop(object sender, DragEventArgs e)
        {
            //check to see what the user dropped on top of us
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //get the files we have dropped
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                //check if we have anything
                if (paths.Length > 0)
                {
                    //filter out files that we don't support
                    //tag reader
                    using (var tagReader = new TagReader())
                    {
                        //add files to the playlist
                        foreach (var path in paths)
                        {
                            if (Directory.Exists(path))
                            {
                                //recursively get all the files in the folder
                                var dirFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

                                foreach (var file in dirFiles)
                                {
                                    //add the file to the playlist
                                    AddFile(tagReader, file);
                                }
                            }
                            else if (File.Exists(path))
                            {
                                //add the file to the playlist
                                AddFile(tagReader, path);
                            }
                        }
                    }

                }
            }
        }

        
        #endregion

        #region Register Commands
        private void RegisterCommands()
        {
            //delete a playlist item
            DeleteItem = new Command((parameter) =>
            {
                //remove item
                App.SoundEngine.RemoveFromNowPlaying(SelectedItem);
            },
            (parameter) =>
            {
                return SelectedItem != null;
            }
            );

            //clear
            ClearNowPlaying = new Command((parameter) =>
            {
                //clear
                App.SoundEngine.ClearNowPlaying();
            });
        }
        #endregion

    }
}
