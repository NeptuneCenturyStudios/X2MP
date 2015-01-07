using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using X2MP.Core;

namespace X2MP.Models
{
    class MainWindowViewModel : BaseViewModel
    {
        #region Properties

        /// <summary>
        /// Reference to main window for this view model
        /// </summary>
        public MainWindow Window { get; private set; }

        private Image _backbuffer;
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

                OnPropertyChanged("Visualization");
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
        /// Begins playback or pauses
        /// </summary>
        public ICommand Play { get; private set; }


        #endregion

        #region Constructor

        public MainWindowViewModel(MainWindow window)
        {
            Window = window;

            Component = null;

            //create the back buffer to render visuals
            CreateBackbuffer();
            RenderVisualization();

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

            Play = new Command((parameter) =>
            {
                App.SoundEngine.PlayOrPause();
            });


        }
        #endregion

        #region Visualization Methods

        /// <summary>
        /// Creates a backbuffer to draw on
        /// </summary>
        private void CreateBackbuffer()
        {
            //if we have to create it again, destroy it
            if (_backbuffer != null)
            {
                _backbuffer.Dispose();
            }

            //create back buffer
            _backbuffer = new Bitmap((int)Window.Width, (int)Window.Height);
        }

        /// <summary>
        /// Renders the visualization to the backbuffer
        /// </summary>
        private void RenderVisualization()
        {
            using (var g = Graphics.FromImage(_backbuffer))
            {
                //clear the drawing surface
                g.Clear(Color.Transparent);

                //draw stuff - test
                g.FillRectangle(Brushes.CornflowerBlue, new Rectangle(10, 10, 100, 100));

                //copy the image to the image we want to present
                using (var pg = Graphics.FromImage(Visualization))
                {
                    //render the back buffer to our visualization
                    pg.DrawImage(_backbuffer, new PointF(0, 0));
                }
            }
        }

        #endregion
    }
}
