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
using X2MP.Core;

namespace X2MP.Models
{
    class MainWindowViewModel : BaseViewModel
    {
        #region Events

        public event EventHandler<VisualizationUpdatedEventArgs> VisualizationUpdated;

        #endregion

        #region Properties
        Graphics g;
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
        /// Gets the command for the now playing button
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

            //create the back buffer to render visuals
            CreateBackbuffer();


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
                var playList = new List<PlayListEntry>();

                foreach (var entry in App.SoundEngine.NowPlaying)
                {
                    playList.Add(entry);
                }

                App.SoundEngine.NeedNextMedia += (sender, e) =>
                {
                    if (playList.Count > 0)
                    {
                        //feed the sound engine
                        var entry = playList[0];

                        e.Media = entry;

                        //remove entry from list
                        playList.Remove(entry);
                    }
                };

                App.SoundEngine.PlayOrPause();

                //start pulling the WaveData
                //Task.Run(() =>
                //{
                //    while (true)
                //    {
                //        RenderVisualization();

                //        Thread.Sleep(25);
                //    }
                //});

                System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler((sender, e) =>
                {
                    RenderVisualization();
                });
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 25);
                dispatcherTimer.Start();


            });

            Stop = new Command((parameter) =>
            {
                App.SoundEngine.Stop();
            });

        }
        #endregion

        #region Visualization Methods



        /// <summary>
        /// Creates a backbuffer to draw on
        /// </summary>
        private void CreateBackbuffer()
        {

            ////if we have to create it again, destroy it
            //if (_backbuffer != null)
            //{
            //    _backbuffer.Dispose();
            //    Visualization.Dispose();
            //}

            ////create back buffer
            //_backbuffer = new Bitmap((int)Window.Width, (int)Window.Height);

            _visualization = new Bitmap((int)Window.Width, (int)Window.Height);

            g = Graphics.FromImage(_visualization);
        }

        /// <summary>
        /// Renders the visualization to the backbuffer
        /// </summary>
        private void RenderVisualization()
        {
            ////using ()
            ////{
            //    //clear the drawing surface
            //    g.Clear(Color.White);

            //    //draw stuff - test
            //    g.FillRectangle(Brushes.CornflowerBlue, new Rectangle(10, 10, 100, 100));

            //    ////copy the image to the image we want to present
            //    //using (var pg = Graphics.FromImage(Visualization))
            //    //{
            //    //    //render the back buffer to our visualization
            //    //    pg.DrawImage(_backbuffer, new PointF(0, 0));
            //    //}
            ////}

            if (App.SoundEngine.WaveData != null)
            {
                //copy the current buffer so we can work with it without fear of it changing,
                //because it will change very, very quickly

                var sampleBuffer = new float[App.SoundEngine.WaveData.Length];

                Array.Copy(App.SoundEngine.WaveData, 0, sampleBuffer, 0, sampleBuffer.Length);
                //var sampleBuffer = (float[])App.SoundEngine.WaveData.Clone();
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
