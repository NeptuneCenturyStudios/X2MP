using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace X2MP.Models
{
    /// <summary>
    /// View model for Now Playing view
    /// </summary>
    class NowPlayingViewModel : BaseViewModel
    {
        #region Properties

        /// <summary>
        /// Gets the currently playing playlist
        /// </summary>
        public ObservableCollection<String> NowPlaying { get; private set; }

        #region Commands
        public ICommand Drop { get; private set; }
        #endregion

        #endregion
        
        #region Constructor
        public NowPlayingViewModel()
        {
            NowPlaying = new ObservableCollection<string>();
        }
        #endregion

        #region Register Commands
        private void RegisterCommands()
        {
            Drop = new Command((parameter) => { });
        }
        #endregion
    }
}
