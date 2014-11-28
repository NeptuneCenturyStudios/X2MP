using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X2MP.Models
{
    /// <summary>
    /// View model for Now Playing view
    /// </summary>
    class NowPlayingViewModel
    {
        #region Properties

        /// <summary>
        /// Gets the currently playing playlist
        /// </summary>
        public ObservableCollection<String> NowPlaying { get; private set; }

        #endregion

        #region Constructor
        public NowPlayingViewModel()
        {
            NowPlaying = new ObservableCollection<string>();
        }
        #endregion
    }
}
