using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace X2MP.Core
{
    public class SoundEngine : IDisposable, INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Fields

        public enum PlaybackControl
        {
            Neutral,
            Pause,
            Stop,
            Next,
            Prev
        }

        /// <summary>
        /// FMOD system object
        /// </summary>
        private FMOD.System _system;

        ///// <summary>
        ///// Stores information to control playback. This structure will be passed to callbacks
        ///// </summary>
        //private SoundSystemControl _control;

        ///// <summary>
        ///// Handle to the SoundSystemControl object
        ///// </summary>
        //private GCHandle _controlHandle;

        /// <summary>
        /// Cancellation token source for cancelling playback
        /// </summary>
        private CancellationTokenSource _playbackCts;

        /// <summary>
        /// Playing channel
        /// </summary>
        //private FMOD.Channel _channel;
        //private List<FMOD.Channel> Channels { get; set; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the internal playlist from which FMOD gets its streams
        /// </summary>
        private List<MediaInfo> InternalPlayList { get; set; }

        /// <summary>
        /// Gets the currently playing playlist.
        /// </summary>
        public PlayList NowPlaying { get; private set; }

        ///// <summary>
        ///// Gets whether the channel is playing
        ///// </summary>
        //public bool IsPlaying
        //{
        //    get
        //    {
        //        if (_channel != null)
        //        {

        //            bool isPlaying;
        //            FMOD.RESULT result;

        //            result = _channel.isPlaying(out isPlaying);
        //            CheckError(result);

        //            //returns whether or not the channel is playing
        //            return isPlaying;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //}
        public bool IsPlaying { get; private set; }
        private PlaybackControl PlayControl { get; set; }

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
        /// Sets the position of the media in milliseconds
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

        #region Constructor / Destructor

        /// <summary>
        /// Start the FMOD sound system engine
        /// </summary>
        public SoundEngine()
        {
            ////create sound system control struct
            //_control = new SoundSystemControl();

            ////create a pinned handle so we can access this object in our callbacks
            //_controlHandle = GCHandle.Alloc(_control, GCHandleType.Pinned);

            //initialize the playlist
            NowPlaying = new PlayList();
            //initialize internal playlist
            InternalPlayList = new List<MediaInfo>();
            //playing channels
            //_channel = null;
            //Channels = new List<FMOD.Channel>();
            _playbackCts = new CancellationTokenSource();


            FMOD.RESULT result;

            //create the system object   
            result = FMOD.Factory.System_Create(out _system);
            CheckError(result);

            //initialize fmod
            result = _system.init(16, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
            CheckError(result);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            //destroy FMOD ';;'
            if (_system != null)
            {
                FMOD.RESULT result;

                //close system
                result = _system.close();
                CheckError(result);

                //release system
                result = _system.release();
                CheckError(result);

                //free handles
                //_controlHandle.Free();

            }
        }

        #endregion

        #region Playback Methods

        /// <summary>
        /// Begins playback or pauses playback
        /// </summary>
        public void PlayOrPause()
        {
            //if the track is playing, then pressing play again will pause the track
            if (IsPlaying)
            {
                //its playing: pause or unpause
                PlayControl = PlaybackControl.Pause;
            }
            else
            {
                //nothing is playing, play something
                var playTask = PlayAsync();
            }
        }

        /// <summary>
        /// Pauses or unpauses playback
        /// </summary>
        private void Pause(FMOD.Channel channel)
        {
            FMOD.RESULT result;
            bool paused;
            result = channel.getPaused(out paused);
            CheckError(result);

            result = channel.setPaused(!paused);
            CheckError(result);

        }

        /// <summary>
        /// Begins playback. This method is called when the user initiates playback.
        /// </summary>
        private async Task PlayAsync()
        {

            //create a new cancellation token
            _playbackCts = new CancellationTokenSource();


            //start a task to prepare the playlist
            await Task.Run(async () =>
            {
                //clear the internal playlist
                InternalPlayList.Clear();

                //create the internal playlist
                foreach (var item in NowPlaying)
                {
                    //add the item from the now playing window to the internal playlist
                    InternalPlayList.Add(new MediaInfo() { FileName = item.FileName });
                }

                //look through the internal playlist
                foreach (var media in InternalPlayList)
                {
                    //check is playback is stopped
                    if (_playbackCts.IsCancellationRequested)
                    {
                        break;
                    }

                    //create the stream from the media info
                    await CreateStreamAsync(media);

                    //play it
                    var channel = PlayStream(media);

                    //now that the sound is playing, we hold the thread hostage.
                    //a variable controls when the thread should be released.
                    await Task.Run(() =>
                    {
                        bool stopping = false;
                        IsPlaying = true;
                        PlayControl = PlaybackControl.Neutral;

                        //get the length of the media
                        Length = GetLength(media);

                        //hold thread hostage while the sound is playing. in the meantime,
                        //update the open state, and update the system so that callbacks will be generated
                        while (IsPlaying)
                        {

                            //we must call this once per frame
                            _system.update();

                            //if cancellation of the playback token is requested, initiate stop
                            if (_playbackCts.IsCancellationRequested && !stopping)
                            {
                                //begin stop
                                var stopTask = StopStreamAsync(channel, media);
                                //prevent from trying to stop again
                                stopping = true;
                            }

                            //check play control and control playback accordingly
                            if (PlayControl == PlaybackControl.Pause && !stopping)
                            {
                                //pause or unpause the playback
                                Pause(channel);

                                //return to resting
                                PlayControl = PlaybackControl.Neutral;
                            }


                            //update the open state of the media
                            media.UpdateOpenState();

                            //get the position
                            Position = GetPosition(channel);

                            //is media still playing
                            //IsPlaying = (media.OpenState == FMOD.OPENSTATE.PLAYING);
                            bool isPlaying;
                            channel.isPlaying(out isPlaying);

                            IsPlaying = isPlaying;

                            //wait
                            Thread.Sleep(10);
                        }

                    }, _playbackCts.Token);


                }

            });
        }

        /// <summary>
        /// Stops playback
        /// </summary>
        public async Task StopAsync()
        {
            //stops playback of the media
            _playbackCts.Cancel();

            await Task.Run(() =>
            {
                //wait until IsPlaying is false
                while (IsPlaying)
                {
                    Thread.Sleep(10);
                }

            });
        }

        /// <summary>
        /// Creates a stream non-blocking
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private async Task CreateStreamAsync(MediaInfo media)
        {

            //result
            FMOD.RESULT result;
            FMOD.CREATESOUNDEXINFO exInfo = new FMOD.CREATESOUNDEXINFO();

            //create a stream non-blocking
            result = this._system.createStream(media.FileName, FMOD.MODE.CREATESTREAM | FMOD.MODE._2D | FMOD.MODE.NONBLOCKING, ref exInfo, out media.Sound);
            CheckError(result);

            //we have to check status to determine when the sound is ready to be played
            await Task.Run(() =>
            {
                //update the open state of the media
                media.UpdateOpenState();
                //check the open state of the media
                while (media.OpenState != FMOD.OPENSTATE.READY && media.OpenState != FMOD.OPENSTATE.ERROR)
                {
                    //update the open state of the media
                    media.UpdateOpenState();

                    //wait
                    Thread.Sleep(1);
                }


            });

        }

        /// <summary>
        /// Takes a media info object and plays it on an open channel
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        private FMOD.Channel PlayStream(MediaInfo media)
        {
            //result
            FMOD.RESULT result;
            FMOD.Channel channel;

            //if (_channel != null)
            //{
            //    //clear callback
            //    //result = _channel.setCallback(null);
            //    //CheckError(result);

            //    //end playback
            //    if (IsPlaying)
            //    {
            //        result = _channel.stop();
            //        CheckError(result);
            //    }
            //}

            //play the sound on the channel
            result = this._system.playSound(media.Sound, null, true, out channel);
            CheckError(result);

            //unpause when ready to begin playing
            result = channel.setPaused(false);
            CheckError(result);

            return channel;
        }

        /// <summary>
        /// Stops the playback of the channel and destroys the channel
        /// </summary>
        private async Task StopStreamAsync(FMOD.Channel channel, MediaInfo media)
        {

            await Task.Run(() =>
            {
                FMOD.RESULT result;
                //fade out
                //TODO replace 1.0 with current volume level
                for (float x = 1.0f; x > 0; x -= 0.05f)
                {
                    //set volume
                    result = channel.setVolume(x);
                    CheckError(result);

                    //sleep
                    Thread.Sleep(25);

                }

                //stop playback
                result = channel.stop();
                CheckError(result);

                //release the sound
                result = media.Sound.release();
                CheckError(result);


            });

        }

        #endregion

        #region Media Query

        /// <summary>
        /// Gets the length of the media in milliseconds
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        private uint GetLength(MediaInfo media)
        {
            //get length
            uint length;
            media.Sound.getLength(out length, FMOD.TIMEUNIT.MS);

            return length;
        }

        /// <summary>
        /// Gets the position of the media playback in milliseconds
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private uint GetPosition(FMOD.Channel channel)
        {
            uint position;
            channel.getPosition(out position, FMOD.TIMEUNIT.MS);

            return position;
        }
        #endregion


        #region Helper Methods

        /// <summary>
        /// Checks the result for errors
        /// </summary>
        /// <param name="result"></param>
        private void CheckError(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                //get error string
                var error = FMOD.Error.String(result);
                //throw the error
                throw new Exception(error);
            }
        }
        #endregion

        //#region Callback

        ///// <summary>
        ///// Handles channel callbacks
        ///// </summary>
        ///// <param name="channelraw"></param>
        ///// <param name="controltype"></param>
        ///// <param name="type"></param>
        ///// <param name="commanddata1"></param>
        ///// <param name="commanddata2"></param>
        ///// <returns></returns>
        //private static FMOD.RESULT ChannelCallback(IntPtr channelraw, FMOD.CHANNELCONTROL_TYPE controltype, FMOD.CHANNELCONTROL_CALLBACK_TYPE type, IntPtr commanddata1, IntPtr commanddata2)
        //{

        //    //create a channel object from a pointer
        //    FMOD.Channel channel = new FMOD.Channel(channelraw);

        //    //detect type of callback
        //    if (type == FMOD.CHANNELCONTROL_CALLBACK_TYPE.END)
        //    {
        //        //get user data
        //        IntPtr data;
        //        FMOD.RESULT result = channel.getUserData(out data);

        //        //get the playback control
        //        var control = (SoundSystemControl)GCHandle.FromIntPtr(data).Target;

        //        //the track is no longer playing. release the hostage!
        //        control.TrackPlaying = false;
        //    }

        //    return FMOD.RESULT.OK;
        //}
        //#endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// Fires the PropertyChanged event
        /// </summary>
        /// <param name="expression"></param>
        protected void OnPropertyChanged(string propertyName)
        {
            //fire property changed event
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion


    }
}
