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
    public class MainWindowViewModel : BaseViewModel
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

                if (_component is VisualizerUserControl)
                {
                    //free
                    var visComponent = (VisualizerUserControl)_component;
                    visComponent.Dispose();
                }
                
                _component = value;

                

                //raise changed event
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
            get
            {
                //get time
                var pos = App.SoundEngine.Position;

                //create a time span object
                CurrentTime = TimeSpan.FromMilliseconds(pos);

                //return time
                return pos;
            }
            set { App.SoundEngine.Position = value; }
        }

        /// <summary>
        /// Gets whether the system is paused or not
        /// </summary>
        public bool IsPlaying
        {
            get { return App.SoundEngine.IsPlaying; }
        }

        private TimeSpan _currentTime;
        public TimeSpan CurrentTime
        {
            get { return _currentTime; }
            private set
            {
                _currentTime = value;

                //raise changed event
                OnPropertyChanged("CurrentTime");
            }
        }

        public bool RepeatOn
        {
            get { return App.SoundEngine.RepeatOn; }
            set
            {
                App.SoundEngine.RepeatOn = value;

                //raise changed event
                OnPropertyChanged("RepeatOn");
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command for the visualizer button
        /// </summary>
        public ICommand OpenVisualizer { get; private set; }

        /// <summary>
        /// Gets the command for the equalizer button
        /// </summary>
        public ICommand OpenEqualizer { get; private set; }

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

        /// <summary>
        /// Go back in history
        /// </summary>
        public ICommand Prev { get; private set; }

        /// <summary>
        /// Go forward in history or get a new song
        /// </summary>
        public ICommand Next { get; private set; }

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
            App.SoundEngine.PropertyChanged += SoundEngine_PropertyChanged;
            App.SoundEngine.PlaybackStatusChanged += SoundEngine_PlaybackStatusChanged;

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
                //display the collection component
                Component = new CollectionUserControl();
            });

            //create commands
            OpenEqualizer = new Command((parameter) =>
            {
                //display the equalizer component
                Component = new EqualizerUserControl();
            });

            //open visualizer
            OpenVisualizer = new Command((parameter) =>
            {

                //display the visualizer component
                Component = new VisualizerUserControl(this);
            });

            //play or pause
            Play = new Command((parameter) =>
            {
                //play or pause the song
                App.SoundEngine.PlayOrPause(null);

            });

            //stop
            Stop = new Command((parameter) =>
            {
                //cancel visualization task
                _visTimer.Stop();

                App.SoundEngine.Stop();
            });

            //prev
            Prev = new Command((parameter) =>
            {
                App.SoundEngine.PlayPrev();
            });

            //next
            Next = new Command((parameter) =>
            {
                App.SoundEngine.PlayNext();
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

        #region Events

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SoundEngine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //raise the event that the property has changed
            OnPropertyChanged(e.PropertyName);
        }

        /// <summary>
        /// Handles when the play back status changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SoundEngine_PlaybackStatusChanged(object sender, EventArgs e)
        {
            if (!IsPlaying)
            {
                //stop the timer
                _visTimer.Stop();
            }
            else if (!_visTimer.IsEnabled)
            {
                //start it
                _visTimer.Start();
            }

        }

        #endregion
    }
}
