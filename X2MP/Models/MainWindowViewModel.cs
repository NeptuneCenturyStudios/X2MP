using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using X2MP.Core;

namespace X2MP.Models
{
    class MainWindowViewModel : BaseViewModel
    {
        #region Events

        public event EventHandler<VisualizationUpdatedEventArgs> VisualizationUpdated;

        #endregion

        #region Fields
        
        private DispatcherTimer _visTimer;
        #endregion

        #region Properties

        /// <summary>
        /// Reference to main window for this view model
        /// </summary>
        public MainWindow Window { get; private set; }

        //private Image _backbuffer;
        private Image _visualization;
        public Image Visualization
        {
            get
            {
                return _visualization;
            }
            private set
            {
                _visualization = value;
            }
        }

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
        public uint Length
        {
            get { return App.SoundEngine.Length; }
        }

        /// <summary>
        /// Gets or sets the position of the media in milliseconds
        /// </summary>
        public uint Position
        {
            get { return App.SoundEngine.Position; }
            set { App.SoundEngine.Position = value; }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command for the now playing button
        /// </summary>
        public ICommand OpenNowPlaying { get; private set; }

        /// <summary>
        /// Gets the command for the collection button
        /// </summary>
        public ICommand OpenCollection { get; private set; }

        /// <summary>
        /// Begins playback or pauses
        /// </summary>
        public ICommand Play { get; private set; }

        /// <summary>
        /// Stops playback
        /// </summary>
        public ICommand Stop { get; private set; }

        #endregion

        #region Constructor

        public MainWindowViewModel(MainWindow window)
        {
            Window = window;

            Component = null;
                       
            //create timer. we are using timer because it performs better than a thread in
            //in this case. uses less CPU.
            _visTimer = new DispatcherTimer();
            _visTimer.Interval = new TimeSpan(0, 0, 0, 0, 25);
            _visTimer.Tick += new EventHandler((sender, e) =>
            {
                RenderVisualization();
            });

            //hook up properties
            App.SoundEngine.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                //raise the event that the property has changed
                OnPropertyChanged(e.PropertyName);
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

            //open collection view
            OpenCollection = new Command((parameter) =>
            {
                //display the Now Playing component
                Component = null;
            });

            Play = new Command((parameter) =>
            {

                App.SoundEngine.PlayOrPause();

                //make sure it is stopped!
                _visTimer.Stop();
                //start it
                _visTimer.Start();


            });

            Stop = new Command((parameter) =>
            {
                //cancel visualization task
                _visTimer.Stop();

                App.SoundEngine.Stop();
            });

        }
        #endregion

        #region Visualization Methods


        /// <summary>
        /// Renders the visualization to the backbuffer
        /// </summary>
        private void RenderVisualization()
        {

            if (App.SoundEngine.WaveData != null)
            {
                //copy the current buffer so we can work with it without fear of it changing,
                //because it will change very, very quickly
                var sampleBuffer = new float[App.SoundEngine.WaveData.Length];

                //copy
                Array.Copy(App.SoundEngine.WaveData, 0, sampleBuffer, 0, sampleBuffer.Length);

                //raise event
                OnVisualizationUpdated(new VisualizationUpdatedEventArgs() { SampleBuffer = sampleBuffer });
            }

        }

        #endregion

        #region Event Handlers
        protected void OnVisualizationUpdated(VisualizationUpdatedEventArgs e)
        {
            var handler = VisualizationUpdated;
            if (handler != null)
            {
                VisualizationUpdated(this, e);
            }
        }
        #endregion
    }
}
