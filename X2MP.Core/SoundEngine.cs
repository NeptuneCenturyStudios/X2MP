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
        public event EventHandler PlaybackStatusChanged;

        #endregion

        #region Fields

        //FMOD system objects
        private FMOD.System _system;                //main system handle
        private FMOD.Sound _sound;                  //sound handle
        private FMOD.Channel _channel;              //channel handle

        private FMOD.DSP _dsp;                      //our DSP unit
        private FMOD.DSP_DESCRIPTION _dspDesc;      //DSP description. this is global because the GC tends to destroy the delegate for the callback, leading to exceptions
        private float[] _sampleBuffer;              //stores a copy of the current frame from the DSP unit

        private List<FMOD.DSP> _eq;                 //an array of equalizer dsp units

        /// <summary>
        /// Cancellation token source for cancelling playback
        /// </summary>
        private CancellationTokenSource _playbackCts;

        private PlayListEntry _playingEntry;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the playlist index
        /// </summary>
        private int _playListIndex;
        private int PlayListIndex
        {
            get
            {
                return _playListIndex;
            }
            set
            {
                //check
                if (value > NowPlaying.Count - 1)
                {
                    value = NowPlaying.Count - 1;
                }

                if (value < -1)
                {
                    value = -1;
                }

                //set index
                _playListIndex = value;
            }
        }

        /// <summary>
        /// Stores a history of what songs have been played automatically
        /// </summary>
        private List<PlayListEntry> History { get; set; }

        /// <summary>
        /// A pointer to the current position in the history buffer
        /// </summary>
        private int HistoryPointer { get; set; }

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

                //raise changed event
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

                //raise changed event
                OnPropertyChanged("Position");
            }
        }

        /// <summary>
        /// Gets whether the system is paused or not
        /// </summary>
        private bool _isPlaying;
        public bool IsPlaying
        {
            get { return _isPlaying; }
            private set
            {
                _isPlaying = value;

                //raise changed event
                OnPropertyChanged("IsPlaying");
            }
        }

        /// <summary>
        /// Indicates that the system is stopping
        /// </summary>
        private bool IsStopping { get; set; }

        public ParamEqInfo[] EqualizerBands { get; private set; }
        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Start the FMOD sound system engine
        /// </summary>
        public SoundEngine()
        {
            //initialize the playlist
            NowPlaying = new PlayList();
            PlayListIndex = -1;

            //create history
            History = new List<PlayListEntry>();

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

            //setup equalizer bands
            InitializeEqualizerBands();

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
        /// <param name="entry">The playlist entry to play. Pass null to play from start</param>
        public void PlayOrPause(PlayListEntry entry)
        {
            //if we have an entry, then stop play back
            if (entry != null && GetIsPlaying())
            {
                //stop playback
                Stop();
            }

            //if the track is playing, then pressing play again will pause the track
            if (GetIsPlaying())
            {
                //its playing: pause or unpause
                Pause();
            }
            else
            {

                if (entry != null)
                {

                    //set playlist index to playing entry
                    PlayListIndex = NowPlaying.IndexOf(entry);

                    //add the entry to the history
                    AddToHistory(entry);
                }

                //run playback
                Run(entry);
            }
        }

        /// <summary>
        /// Run playback
        /// </summary>
        /// <param name="entry"></param>
        private void Run(PlayListEntry entry = null)
        {
            //check to see if we have a reference to the last entry
            if (_playingEntry != null)
            {
                //this entry is not playing any more
                _playingEntry.IsPlaying = false;
            }

            //get next media if entry is null
            entry = entry ?? GetNextMedia();

            if (entry != null)
            {
                //keep a global reference to the playing entry
                _playingEntry = entry;

                //set is playing on the entry
                entry.IsPlaying = true;

                //nothing is playing, play something
                Play(entry);
            }
        }

        /// <summary>
        /// Prepares media for playback
        /// </summary>
        /// <param name="filename"></param>
        private void LoadMedia(PlayListEntry entry)
        {
            FMOD.RESULT result;

            //release any playing sound
            if (_sound != null)
            {
                _sound.release();
            }

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
        private void Play(PlayListEntry entry)
        {
            if (entry == null)
            {
                //throw an error
                throw new ArgumentNullException("entry");
            }

            //set playing
            IsPlaying = true;

            //create new cancellation token
            _playbackCts = new CancellationTokenSource();

            //create a play task
            var playTask = Task.Run(() =>
            {
                //send the media to be played
                LoadMedia(entry);

                //play the stream. holds thread hostage until playback stops
                PlayStream();

            }, _playbackCts.Token);


            //when playback is stopped, handle what to do next
            //either we exit and go idle, or we pick the next track.
            //if the user selected a different entry to play, we treat it like a hard stop.
            playTask.ContinueWith((t) =>
            {
                //if canceled, exit
                if (IsStopping)
                {
                    //reset
                    IsStopping = false;
                    //exit
                    return;
                }

                //play the next song
                Run();
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

            //stop any playing channels
            if (_channel != null)
            {
                _channel.stop();
            }

            //free DSP
            if (_dsp != null)
            {
                _dsp.release();
            }

            //destroy the equalizer
            DestroyEqualizer();

            //play the sound on the channel
            result = _system.playSound(_sound, null, true, out _channel);
            CheckError(result);

            //prepare the equalizer
            SetupEqualizer();

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

            //fire PlaybackStatusChanged event
            OnPlaybackStatusChanged();

            //hold the thread hostage
            while (GetIsPlaying())
            {
                //check to see if playback should stop
                if (_playbackCts.IsCancellationRequested)
                {
                    //we requested to stop playback
                    break;
                }

                //get the current position
                InternalPosition = GetPosition();

                //update fmod
                Update();

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

            //get paused status
            result = _channel.getPaused(out paused);
            CheckError(result);

            //unpause or pause
            result = _channel.setPaused(!paused);
            CheckError(result);

            //set IsPaused property to notify listeners
            IsPlaying = paused;

            //fire PlaybackStatusChanged event
            OnPlaybackStatusChanged();

        }

        /// <summary>
        /// Stops playback
        /// </summary>
        public void Stop(bool resetHistory = true)
        {
            //this entry is not playing any more
            if (_playingEntry != null)
            {
                _playingEntry.IsPlaying = false;

            }

            //set the flag that we are stopping playback
            IsStopping = true;

            //set public flag to indicate we are not playing
            IsPlaying = false;

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

            //clear history
            if (resetHistory)
            {
                History.Clear();
                HistoryPointer = 0;
                PlayListIndex = -1;
            }

            //fire PlaybackStatusChanged event
            OnPlaybackStatusChanged();
        }

        #endregion

        #region Equalizer

        /// <summary>
        /// Initializes the equalizer band values, center freq, bandwidth, gain, etc...
        /// </summary>
        private void InitializeEqualizerBands()
        {
            var min = -6.0f;   //min can be as low as -30, but we dont want that for now
            var max = 7.0f;    //max can be as high as 30, but we dont want that for now
            var gain = 1.0f;    //fmod's default gain value, means no modification to sound

            EqualizerBands = new[] { 
                new ParamEqInfo() { Center = 31.5f    , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 62.5f    , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 125.0f   , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 250.0f   , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 500.0f   , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 1000.0f  , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 2000.0f  , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 4000.0f  , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 8000.0f  , Min=min, Max=max, Gain=gain },
                new ParamEqInfo() { Center = 16000.0f , Min=min, Max=max, Gain=gain }
            };

            //create instance of list
            _eq = new List<FMOD.DSP>();
        }

        /// <summary>
        /// Add the DSP units to the channel
        /// </summary>
        private void SetupEqualizer()
        {

            FMOD.RESULT result;

            foreach (var eq in EqualizerBands)
            {
                FMOD.DSP eqDsp;

                //create the equalizer dps effects
                result = _system.createDSPByType(FMOD.DSP_TYPE.PARAMEQ, out eqDsp);

                //set up the dsp unit
                result = eqDsp.setParameterFloat((int)FMOD.DSP_PARAMEQ.CENTER, eq.Center);

                //set the gain to the current gain
                SetEqualizerBand(eqDsp, eq.Gain);

                //prepare the equalizer on the channel
                result = _channel.addDSP(0, eqDsp);

                //add dsp to list
                _eq.Add(eqDsp);
            }

        }

        /// <summary>
        /// Releases the DSP units that make up the equalizer
        /// </summary>
        private void DestroyEqualizer()
        {
            foreach (var eqDsp in _eq)
            {
                //release the dsp unit
                eqDsp.release();
            }

            //clear dsp
            _eq.Clear();
        }

        /// <summary>
        /// Sets the gain of the specific equalizer band
        /// </summary>
        /// <param name="index"></param>
        /// <param name="gain"></param>
        public void SetEqualizerBand(int index, float gain)
        {
            if (index < _eq.Count && index >= 0)
            {
                SetEqualizerBand(_eq[index], gain);
            }
        }

        private void SetEqualizerBand(FMOD.DSP eqDsp, float gain)
        {
            FMOD.RESULT result;
            //set the gain for the specified eq band
            result = eqDsp.setParameterFloat((int)FMOD.DSP_PARAMEQ.GAIN, gain);
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

            if (_channel != null)
            {
                //set position
                result = _channel.setPosition(position, FMOD.TIMEUNIT.MS);
                CheckError(result);
            }
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

        }

        /// <summary>
        /// Removes an entry from the playlist
        /// </summary>
        /// <param name="entry"></param>
        public void RemoveFromNowPlaying(PlayListEntry entry)
        {
            //remove
            NowPlaying.Remove(entry);

            if (entry == _playingEntry)
            {
                //stop playing
                Stop();
            }

        }

        /// <summary>
        /// Clear the now playing list
        /// </summary>
        public void ClearNowPlaying()
        {
            NowPlaying.Clear();

            //stop playback
            Stop();
        }

        /// <summary>
        /// Adds an entry to the history
        /// </summary>
        /// <param name="entry"></param>
        private void AddToHistory(PlayListEntry entry)
        {
            //push entry into the history
            History.Add(entry);

            //increase the history pointer
            HistoryPointer = History.IndexOf(entry);
        }

        /// <summary>
        /// Gets the next media in the list
        /// </summary>
        /// <returns></returns>
        private PlayListEntry GetNextMedia()
        {
            if (NowPlaying.Count > 0 && PlayListIndex + 1 < NowPlaying.Count)
            {
                PlayListEntry entry = null;

                if (HistoryPointer < History.Count - 1)
                {
                    //increase the history pointer
                    HistoryPointer++;

                    //get the playlist entry from the current index
                    entry = History[HistoryPointer];

                }
                else
                {
                    //set next position
                    PlayListIndex++;

                    //get the playlist entry from the current index
                    entry = NowPlaying[PlayListIndex];

                    //add to history
                    AddToHistory(entry);
                }

                //return the entry we got
                return entry;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Plays the previous song in the history
        /// </summary>
        public void PlayPrev()
        {
            //check
            if (--HistoryPointer < 0)
            {
                HistoryPointer = 0;
            }

            if (HistoryPointer < History.Count)
            {
                //grab the last entry in the list
                var entry = History[HistoryPointer];

                //Stop
                Stop(false);

                //play the entry
                Run(entry);
            }


        }

        /// <summary>
        /// Plays the next song in the list
        /// </summary>
        public void PlayNext()
        {
            //stop playback
            Stop(false);

            //Play the next song in the list
            Run();
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

        /// <summary>
        /// Fires the OnPlaybackStatusChanged event
        /// </summary>
        protected void OnPlaybackStatusChanged()
        {
            //fire event
            var handler = PlaybackStatusChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }


        #endregion


    }
}
