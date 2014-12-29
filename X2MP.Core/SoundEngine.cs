using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace X2MP.Core
{
    public class SoundEngine : IDisposable
    {

        /// <summary>
        /// FMOD system object
        /// </summary>
        private FMOD.System _system;

        /// <summary>
        /// Stores information to control playback. This structure will be passed to callbacks
        /// </summary>
        private SoundSystemControl _control;

        /// <summary>
        /// Handle to the SoundSystemControl object
        /// </summary>
        private GCHandle _controlHandle;

        /// <summary>
        /// Playing channel
        /// </summary>
        private FMOD.Channel _channel;
        //private List<FMOD.Channel> Channels { get; set; }

        /// <summary>
        /// Gets or sets the internal playlist from which FMOD gets its streams
        /// </summary>
        private List<MediaInfo> InternalPlayList { get; set; }

        /// <summary>
        /// Gets the currently playing playlist.
        /// </summary>
        public PlayList NowPlaying { get; private set; }

        /// <summary>
        /// Start the FMOD sound system engine
        /// </summary>
        public SoundEngine()
        {
            //create sound system control struct
            _control = new SoundSystemControl();

            //create a pinned handle so we can access this object in our callbacks
            _controlHandle = GCHandle.Alloc(_control, GCHandleType.Pinned);

            //initialize the playlist
            NowPlaying = new PlayList();
            //initialize internal playlist
            InternalPlayList = new List<MediaInfo>();
            //playing channels
            _channel = null;
            //Channels = new List<FMOD.Channel>();

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

                //stop playback
                //Stop();

                //close system
                result = _system.close();
                CheckError(result);

                //release system
                result = _system.release();
                CheckError(result);

                //free handles
                _controlHandle.Free();
            }
        }

        #region Playback Methods

        /// <summary>
        /// Begins playback or pauses playback
        /// </summary>
        public void PlayOrPause()
        {
            //if the track is playing, then pressing play again will pause the track
            if (_control.TrackPlaying)
            {
                //its playing: pause or unpause
                Pause();
            }
            else
            {
                //nothing is playing, play something
                Play();
            }
        }

        /// <summary>
        /// Pauses or unpauses playback
        /// </summary>
        private void Pause()
        {
            FMOD.RESULT result;

            if (_channel != null)
            {
                bool paused;
                result = _channel.getPaused(out paused);
                CheckError(result);

                result = _channel.setPaused(!paused);
                CheckError(result);
            }
        }

        /// <summary>
        /// Begins playback. This method is called when the user initiates playback.
        /// </summary>
        private void Play()
        {
            //start a task to prepare the playlist
            var t = Task.Run(async () =>
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
                    //create the stream from the media info
                    await CreateStream(media);

                    //play it
                    await PlayStream(media);
                }

            });
        }

        /// <summary>
        /// Creates a stream non-blocking
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private async Task CreateStream(MediaInfo media)
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
        private async Task PlayStream(MediaInfo media)
        {
            //result
            FMOD.RESULT result;
            FMOD.Channel channel;

            result = this._system.playSound(media.Sound, null, true, out channel);
            CheckError(result);

            //add the channel to the list so we can work with it later
            //Channels.Add(channel);
            _channel = channel;

            //create callback
            result = channel.setCallback(ChannelCallback);
            CheckError(result);

            //get pointer to class
            var ctrlPtr = GCHandle.ToIntPtr(_controlHandle);
            //set user data
            result = channel.setUserData(ctrlPtr);
            CheckError(result);

            //unpause when ready to begin playing
            result = channel.setPaused(false);

            //now that the sound is playing, we hold the thread hostage.
            //a variable controls when the thread should be released.
            await Task.Run(() =>
            {
                //our music is playing now
                _control.TrackPlaying = true;
                //hold thread hostage while the sound is playing. in the meantime,
                //update the open state, and update the system so that callbacks will be generated
                while (_control.TrackPlaying)
                {
                    //update the open state of the media
                    media.UpdateOpenState();

                    //we must call this once per frame
                    _system.update();

                    //wait
                    Thread.Sleep(1);
                }

            });
        }

        #endregion

        #region Helper Methods
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

        #region Callback

        /// <summary>
        /// Handles channel callbacks
        /// </summary>
        /// <param name="channelraw"></param>
        /// <param name="controltype"></param>
        /// <param name="type"></param>
        /// <param name="commanddata1"></param>
        /// <param name="commanddata2"></param>
        /// <returns></returns>
        private static FMOD.RESULT ChannelCallback(IntPtr channelraw, FMOD.CHANNELCONTROL_TYPE controltype, FMOD.CHANNELCONTROL_CALLBACK_TYPE type, IntPtr commanddata1, IntPtr commanddata2)
        {

            //create a channel object from a pointer
            FMOD.Channel channel = new FMOD.Channel(channelraw);

            //detect type of callback
            if (type == FMOD.CHANNELCONTROL_CALLBACK_TYPE.END)
            {
                //get user data
                IntPtr data;
                FMOD.RESULT result = channel.getUserData(out data);

                //get the playback control
                var control = (SoundSystemControl)GCHandle.FromIntPtr(data).Target;

                //the track is no longer playing. release the hostage!
                control.TrackPlaying = false;
            }

            return FMOD.RESULT.OK;
        }
        #endregion
    }
}
