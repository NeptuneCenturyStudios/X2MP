using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X2MP.Core
{
    /// <summary>
    /// Reads the tag information in an audio file
    /// </summary>
    public class TagReader
    {

        FMOD.System _system;

        #region TagFrames
        public struct ASFFrames
        {
            public const string ARTIST = "WM/AlbumArtist";
        }

        public struct ID3v1Frames
        {
            public const string ARTIST = "ARTIST";
        }

        public struct ID3v2Frames
        {
            public const string ARTIST = "TPE1";
        }
        #endregion

        
        /// <summary>
        /// Constructor
        /// </summary>
        public TagReader()
        {

            
            FMOD.RESULT result;

            //create an fmod instance to use for tag reading
            result = FMOD.Factory.System_Create(out _system);
            CheckError(result);
            result = _system.init(1, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
            CheckError(result);

            
        }

        /// <summary>
        /// Reads the tag of a file
        /// </summary>
        /// <param name="filename"></param>
        public TagInfo ReadTags(string filename){
            //if ok, create a sound with tag support
            FMOD.RESULT result;
            FMOD.Sound snd;
            //open the sound for tag reading
            result = _system.createStream(filename, FMOD.MODE.OPENONLY, out snd);
            CheckError(result);

            //if ok, get the number of tags in the fie
            int numTags;
            int numTagsUpdated;

            //get num tags
            result = snd.getNumTags(out numTags, out numTagsUpdated);
            CheckError(result);
            //if ok, and we have some tags, then

            return ReadTags(snd, numTags);
        }

        /// <summary>
        /// Reads the tags from the stream
        /// </summary>
        /// <param name="snd"></param>
        /// <param name="numTags"></param>
        private TagInfo ReadTags(FMOD.Sound snd, int numTags)
        {
            FMOD.RESULT result;
            var tagInfo = new TagInfo();
            //var tagList = new List<FMOD.TAG>();

            for (var x = 0; x < numTags; x++)
            {
                FMOD.TAG tag;
                //get tag
                result = snd.getTag(null, x, out tag);
                CheckError(result);

                //get the tag info from the song
                SetSongInfo(tag, tagInfo);

                //add to list
                //tagList.Add(tag);
            }

            //return the tag info
            return tagInfo;

        }

        /// <summary>
        /// Gets the tag info from a specific tag
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="tagInfo"></param>
        /// <returns></returns>
        private void SetSongInfo(FMOD.TAG tag, TagInfo tagInfo)
        {
            
            string data = null;

            if (tag.datatype == FMOD.TAGDATATYPE.STRING)
            {
                data = Marshal.PtrToStringAnsi(tag.data);
            }
            else if (tag.datatype == FMOD.TAGDATATYPE.STRING_UTF16)
            {
                data = Marshal.PtrToStringUni(tag.data);
            }
            

            if (tag.type == FMOD.TAGTYPE.ASF)
            {
                //where does it go?
                switch (tag.name)
                {
                    case ASFFrames.ARTIST:
                        tagInfo.Artist = data;
                        break;

                    default:
                        break;
                }
            }
            else if (tag.type == FMOD.TAGTYPE.ID3V1 || tag.type == FMOD.TAGTYPE.ID3V2)
            {

                //get ID3 v1 tags first. these are usually replaced with ID3v2, unless ID3v2 doesn't exist
                if (tag.type == FMOD.TAGTYPE.ID3V1)
                {
                    //where does it go?
                    switch (tag.name)
                    {
                        case ID3v1Frames.ARTIST:
                            tagInfo.Artist = data;
                            break;

                        default:
                            break;
                    }
                }
            }

            
        }

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
