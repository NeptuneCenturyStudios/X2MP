using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace X2MP.Core
{
    public class SoundEngine : IDisposable
    {

        FMOD.System _system;

        /// <summary>
        /// Gets the currently playing playlist.
        /// </summary>
        public PlayList NowPlaying { get; private set; }

        /// <summary>
        /// Gets or sets the internal playlist from which FMOD gets its streams
        /// </summary>
        private List<MediaInfo> InternalPlayList { get; set; }

        /// <summary>
        /// Start the FMOD sound system engine
        /// </summary>
        public SoundEngine()
        {

            //initialize the playlist
            NowPlaying = new PlayList();
            //initialize internal playlist
            InternalPlayList = new List<MediaInfo>();

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
            }
        }

        #region Playback Methods

        /// <summary>
        /// Begins playback
        /// </summary>
        public void Play()
        {
            //start looking at the playlist and prepare the internal playlist
            var t = Task.Run(async () =>
            {
                //create the internal playlist
                foreach (var item in NowPlaying)
                {
                    InternalPlayList.Add(new MediaInfo() { FileName = item });
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

            //create a stream non-blocking
            result = this._system.createStream(media.FileName, FMOD.MODE.CREATESTREAM | FMOD.MODE._2D | FMOD.MODE.NONBLOCKING, out media.Sound);
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

        private async Task PlayStream(MediaInfo media)
        {
            //result
            FMOD.RESULT result;
            FMOD.Channel channel;

            result = this._system.playSound(media.Sound, null, false, out channel);
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
    }
}
