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
        public event EventHandler<NeedNextMediaEventArgs> NeedNextMedia;

        #endregion

        #region Fields

        //FMOD system objects
        private FMOD.System _system;                //main system handle
        private FMOD.Sound _sound;                  //sound handle
        private FMOD.Channel _channel;              //channel handle

        private FMOD.DSP _dsp;                      //our DSP unit
        private FMOD.DSP_DESCRIPTION _dspDesc;      //DSP description. this is global because the GC tends to destroy the delegate for the callback, leading to exceptions
        private float[] _sampleBuffer;              //stores a copy of the current frame from the DSP unit

        /// <summary>
        /// Cancellation token source for cancelling playback
        /// </summary>
        private CancellationTokenSource _playbackCts;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the internal playlist from which FMOD gets its streams
        /// </summary>
        private List<PlayListEntry> InternalPlayList { get; set; }

        /// <summary>
        /// Gets the current wave data frame from the DSP unit
        /// </summary>
        public float[] WaveData { get { return _sampleBuffer; } }

        /// <summary>
        /// Gets the currently playing playlist.
        /// </summary>
        public PlayList NowPlaying { get; private set; }

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
            get { return InternalPosition; }
            set
            {
                InternalPosition = value;

                //set position on the channel
                SetPostion(value);
            }
        }

        /// <summary>
        /// We update this from within the playback loop thread.
        /// This notifies listeners of changes for the "Position" property
        /// </summary>
        private uint InternalPosition
        {
            get { return _position; }
            set
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
            //initialize the playlist
            NowPlaying = new PlayList();
            //initialize internal playlist
            InternalPlayList = new List<PlayListEntry>();
            //create cancellation token
            _playbackCts = new CancellationTokenSource();

            FMOD.RESULT result;

            //create the system object   
            result = FMOD.Factory.System_Create(out _system);
            CheckError(result);

            //initialize fmod
            result = _system.init(32, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
            CheckError(result);

            //create a dsp description for a dsp unit.
            //this dsp unit will allow us to tap into the buffer and get some waveform
            //data for visualizations
            _dspDesc = new FMOD.DSP_DESCRIPTION();
            _dspDesc.name = new char[32];
            _dspDesc.version = 0x00010000;
            _dspDesc.numinputbuffers = 1;
            _dspDesc.numoutputbuffers = 1;
            _dspDesc.read = myDSPCallback;

        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            //destroy FMOD
            if (_system != null)
            {
                FMOD.RESULT result;

                //close system
                result = _system.close();
                CheckError(result);

                //release system
                result = _system.release();
                CheckError(result);

            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Callback for the DSP unit
        /// </summary>
        /// <param name="dsp_state"></param>
        /// <param name="inbuffer"></param>
        /// <param name="outbuffer"></param>
        /// <param name="length"></param>
        /// <param name="inchannels"></param>
        /// <param name="outchannels"></param>
        /// <returns></returns>
        FMOD.RESULT myDSPCallback(ref FMOD.DSP_STATE dsp_state, IntPtr inbuffer, IntPtr outbuffer, uint length, int inchannels, ref int outchannels)
        {

            //getting unsafe here to handle raw pointers to the buffer
            unsafe
            {
                //create raw pointers for the buffers
                float* outBuff = (float*)outbuffer.ToPointer();
                float* inBuff = (float*)inbuffer.ToPointer();

                for (uint samp = 0; samp < length; samp++)
                {
                    //assumes in channels is the same as out channels
                    for (int chan = 0; chan < outchannels; chan++)
                    {
                        //all we are doing in this DSP is pumping the input to the output,
                        //and copying the sample to a buffer we can read for visualizations
                        outBuff[(samp * outchannels) + chan] = inBuff[(samp * inchannels) + chan];
                    }
                }

                //length of the buffer
                var bufferLength = (length * outchannels);

                //create a buffer if we don't have one, or if the length has changed.
                if (_sampleBuffer == null || _sampleBuffer.Length != bufferLength)
                {
                    //create new instance of the buffer
                    _sampleBuffer = new float[bufferLength];
                }

                //copy outbuffer to our sample buffer
                Marshal.Copy(outbuffer, _sampleBuffer, 0, _sampleBuffer.Length);

            }

            //return ok
            return FMOD.RESULT.OK;
        }
        #endregion
        
        #region Playback Methods

        /// <summary>
        /// Updates the FMOD sound system
        /// </summary>
        private void Update()
        {
            _system.update();
        }

        /// <summary>
        /// Begins playback or pauses playback
        /// </summary>
        public void PlayOrPause()
        {
            //if the track is playing, then pressing play again will pause the track
            if (GetIsPlaying())
            {
                //its playing: pause or unpause
                Pause();
            }
            else
            {
                //build a playlist
                BuildInternalPlaylist();
                //nothing is playing, play something
                Play();
            }
        }

        /// <summary>
        /// Prepares media for playback
        /// </summary>
        /// <param name="filename"></param>
        public void LoadMedia(PlayListEntry entry)
        {
            FMOD.RESULT result;

            //if we are already playing, we have to stop the current media

            //load a new sound
            result = _system.createSound(entry.FileName, FMOD.MODE._2D | FMOD.MODE.NONBLOCKING | FMOD.MODE.CREATESTREAM, out _sound);
            CheckError(result);

            //how to determine the media is ready? an event perhaps?
            FMOD.OPENSTATE openState;
            uint percentBuffered;
            bool starving;
            bool diskBusy;

            //check to see if it is ready
            _sound.getOpenState(out openState, out percentBuffered, out starving, out diskBusy);

            //check state
            while (openState == FMOD.OPENSTATE.LOADING)
            {
                _sound.getOpenState(out openState, out percentBuffered, out starving, out diskBusy);
                Thread.Sleep(25);
            }

            Length = GetLength();
        }

        /// <summary>
        /// Begins playback. This method is called when the user initiates playback.
        /// </summary>
        private void Play()
        {

            //get some media to play
            var entry = GetNextMedia();

            //check to see if there are
            if (entry == null)
            {
                return;
            }

            //create new cancellation token
            _playbackCts = new CancellationTokenSource();

            //create a play task
            var playTask = Task.Run(() =>
            {
                //send the media to be played
                LoadMedia(entry);
                //play the stream
                PlayStream();
            }, _playbackCts.Token);

            playTask.ContinueWith((t) =>
            {
                if (_playbackCts.IsCancellationRequested)
                {
                    return;
                }
                //play the next song
                Play();
            });


        }

        /// <summary>
        /// Takes a media info object and plays it on an open channel
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        private void PlayStream()
        {
            //result
            FMOD.RESULT result;

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
            result = _system.playSound(_sound, null, true, out _channel);
            CheckError(result);

            //create the dsp, although it will not be active at this time
            result = _system.createDSP(ref _dspDesc, out _dsp);

            ////deactivate
            //_dsp.setBypass(true);

            //add to dsp chain
            _channel.addDSP(0, _dsp);

            //unpause when ready to begin playing
            result = _channel.setPaused(false);
            CheckError(result);

            ////activate
            //_dsp.setBypass(false);

            //hold the thread hostage
            while (GetIsPlaying())
            {
                //check to see if playback should stop
                if (_playbackCts.IsCancellationRequested)
                {
                    //we requested to stop playback
                    break;
                }

                //update fmod
                Update();

                //get the current position
                InternalPosition = GetPosition();

                //sleep for a few miliseconds
                Thread.Sleep(25);

            }


        }

        /// <summary>
        /// Pauses or unpauses playback
        /// </summary>
        private void Pause()
        {
            FMOD.RESULT result;
            bool paused;
            result = _channel.getPaused(out paused);
            CheckError(result);

            result = _channel.setPaused(!paused);
            CheckError(result);

        }

        /// <summary>
        /// Stops playback
        /// </summary>
        public void Stop()
        {
            //cancel
            _playbackCts.Cancel();

            //stop playback
            if (_channel != null)
            {
                _channel.stop();
                _channel = null;

                _sound.release();
                _sound = null;
            }


        }



        #endregion

        #region Media Query

        //public void UpdateOpenState()
        //{
        //    if (Sound != null)
        //    {
        //        FMOD.OPENSTATE openState;
        //        uint percentBuffered;
        //        bool starving;
        //        bool diskBusy;

        //        //check to see if it is ready
        //        Sound.getOpenState(out openState, out percentBuffered, out starving, out diskBusy);

        //        //set the open state
        //        OpenState = openState;
        //    }
        //}

        /// <summary>
        /// Gets whether the channel is currently playing
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private bool GetIsPlaying()
        {
            if (this._channel == null) return false;

            FMOD.RESULT result;
            bool isPlaying = true;

            //get is playing
            result = _channel.isPlaying(out isPlaying);
            //CheckError(result);

            return isPlaying;
        }

        /// <summary>
        /// Gets the length of the media in milliseconds
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        private uint GetLength()
        {

            FMOD.RESULT result;
            uint length;

            //get length
            result = _sound.getLength(out length, FMOD.TIMEUNIT.MS);
            CheckError(result);

            return length;
        }

        /// <summary>
        /// Gets the position of the media playback in milliseconds
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        private uint GetPosition()
        {
            FMOD.RESULT result;
            uint position;

            //get position
            result = _channel.getPosition(out position, FMOD.TIMEUNIT.MS);
            CheckError(result);

            return position;
        }

        /// <summary>
        /// Sets the position of the playing stream
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="position"></param>
        private void SetPostion(uint position)
        {
            FMOD.RESULT result;

            //set position
            result = _channel.setPosition(position, FMOD.TIMEUNIT.MS);
            CheckError(result);
        }

        #endregion

        #region Playlist Methods
        
        /// <summary>
        /// Adds a new entry to the now playing list, and adds it to the internal playlist if currently playing
        /// </summary>
        /// <param name="entry"></param>
        public void AddToNowPlaying(PlayListEntry entry)
        {
            //add media entry to the now playing playlist
            NowPlaying.Add(entry);

            //if we are currently playing, we need to add this to the internal
            //playlist as well
            if (GetIsPlaying())
            {
                //add to internal playlist
                InternalPlayList.Add(entry);
            }
        }

        /// <summary>
        /// Removes an entry from the playlist
        /// </summary>
        /// <param name="entry"></param>
        public void RemoveFromNowPlaying(PlayListEntry entry)
        {
            //remove
            NowPlaying.Remove(entry);

            //if we are currently playing, we need to add this to the internal
            //playlist as well
            if (GetIsPlaying())
            {
                //add to internal playlist
                InternalPlayList.Remove(entry);

                //stop playback if this is the entry we are currently playing
            }
        }

        /// <summary>
        /// Builds the internal playlist based on settings like Randomize, etc...
        /// </summary>
        private void BuildInternalPlaylist()
        {
            foreach (var entry in NowPlaying)
            {
                InternalPlayList.Add(entry);
            }
        }

        /// <summary>
        /// Gets the next media in the list
        /// </summary>
        /// <returns></returns>
        private PlayListEntry GetNextMedia()
        {
            if (InternalPlayList.Count > 0)
            {
                //feed the sound engine
                var entry = InternalPlayList[0];
                //remove entry from list
                InternalPlayList.Remove(entry);

                return entry;
            }
            else
            {
                return null;
            }
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

        #region Event Handlers

        /// <summary>
        /// Fires the PropertyChanged event
        /// </summary>
        /// <param name="expression"></param>
        protected void OnPropertyChanged(string propertyName)
        {
            //fire event
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //protected void OnNeedNextMedia(NeedNextMediaEventArgs e)
        //{
        //    //fire event
        //    var handler = NeedNextMedia;
        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}

        #endregion


    }
}
