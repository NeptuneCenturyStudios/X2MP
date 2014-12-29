using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace X2MP.Models
{
    class MainWindowViewModel : BaseViewModel
    {
        #region Properties

        public MainWindow Window { get; private set; }

        /// <summary>
        /// Gets the current visible component e.g. Now Playing
        /// </summary>
        private FrameworkElement _component;
        public FrameworkElement Component
        {
            get
            {
                return _component;
            }
            private set
            {
                _component = value;

                //property changes
                OnPropertyChanged(() => this.Component);
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command for the now playing button
        /// </summary>
        public ICommand OpenNowPlaying { get; private set; }

        public ICommand Play { get; private set; }

        #endregion

        #region Constructor

        public MainWindowViewModel(MainWindow window)
        {
            Window = window;

            Component = null;

            //create commands
            RegisterCommands();
        }

        #endregion

        #region Register Commands
        private void RegisterCommands()
        {
            //create commands
            OpenNowPlaying = new Command((parameter) =>
            {
                //display the Now Playing component
                Component = new NowPlayingUserControl();
            });

            Play = new Command((parameter) => {
                App.SoundEngine.PlayOrPause();
            });
        }
        #endregion

    }
}
