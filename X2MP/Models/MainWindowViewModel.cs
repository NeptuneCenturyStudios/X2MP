using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        /// <summary>
        /// Gets the length of the media in milliseconds
        /// </summary>
        private uint _length;
        public uint Length
        {
            get { return _length; }
            private set
            {
                _length = value;

                OnPropertyChanged("Length");
            }
        }

        /// <summary>
        /// Gets or sets the position of the media in milliseconds
        /// </summary>
        private uint _position;
        public uint Position
        {
            get { return _position; }
            private set
            {
                _position = value;

                OnPropertyChanged("Position");
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command for the now playing button
        /// </summary>
        public ICommand OpenNowPlaying { get; private set; }

        /// <summary>
        /// Begins playback or pauses
        /// </summary>
        public ICommand Play { get; private set; }


        #endregion

        #region Constructor

        public MainWindowViewModel(MainWindow window)
        {
            Window = window;

            Component = null;

            //hook up properties
            App.SoundEngine.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                //when the property changes, get its new value
                var srcProp = App.SoundEngine.GetType().GetProperty(e.PropertyName);

                var value = srcProp.GetValue(App.SoundEngine);

                var dstProp = this.GetType().GetProperty(e.PropertyName);

                dstProp.SetValue(this, value);
            };

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

            Play = new Command((parameter) =>
            {
                App.SoundEngine.PlayOrPause();
            });


        }
        #endregion

    }
}
