﻿using System;
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
    public class TagReader : IDisposable
    {

        FMOD.System _system;

        #region TagFrames
        public struct ASFFrames
        {
            public const string ARTIST = "WM/AlbumArtist";
            public const string ALBUM = "WM/AlbumTitle";
            public const string TITLE = "TITLE";
            public const string TRACK = "WM/TrackNumber";
        }

        public struct ID3v1Frames
        {
            public const string ARTIST = "ARTIST";
            public const string ALBUM = "ALBUM";
            public const string TITLE = "TITLE";
            public const string TRACK = "TRACK";
        }

        public struct ID3v2Frames
        {
            public const string ARTIST = "TPE1";
            public const string ALBUM = "TALB";
            public const string TITLE = "TIT2";
            public const string TRACK = "TRCK";
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
        public TagInfo ReadTags(string filename)
        {
            FMOD.Sound snd = null;
            TagInfo tagInfo = null;

            try
            {
                //if ok, create a sound with tag support
                FMOD.RESULT result;

                //open the sound for tag reading
                result = _system.createStream(filename, FMOD.MODE.OPENONLY, out snd);
                CheckError(result);

                //if ok, get the number of tags in the fie
                int numTags;
                int numTagsUpdated;

                //get num tags
                result = snd.getNumTags(out numTags, out numTagsUpdated);
                CheckError(result);
                
                //read the tags
                tagInfo = ReadTags(snd, numTags);


            }
            catch (Exception)
            {
                //ignore errors for now
            }
            finally
            {
                if (snd != null)
                {
                    //release the sound
                    snd.release();
                    snd = null;
                }
            }

            //return the tag info
            return tagInfo;
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
            else if (tag.datatype == FMOD.TAGDATATYPE.INT)
            {
                data = Marshal.ReadInt32(tag.data).ToString();
            }


            if (tag.type == FMOD.TAGTYPE.ASF)
            {
                //where does it go?
                switch (tag.name)
                {
                    case ASFFrames.ARTIST:
                        tagInfo.Artist = data;
                        break;
                    case ASFFrames.ALBUM:
                        tagInfo.Album = data;
                        break;
                    case ASFFrames.TITLE:
                        tagInfo.Title = data;
                        break;
                    case ASFFrames.TRACK:
                        tagInfo.Track = data;
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
                        case ID3v1Frames.ALBUM:
                            tagInfo.Album = data;
                            break;
                        case ID3v1Frames.TITLE:
                            tagInfo.Title = data;
                            break;
                        case ID3v1Frames.TRACK:
                            tagInfo.Track = data;
                            break;
                        default:
                            break;
                    }
                }

                if (tag.type == FMOD.TAGTYPE.ID3V2)
                {
                    //where does it go?
                    switch (tag.name)
                    {
                        case ID3v2Frames.ARTIST:
                            tagInfo.Artist = data;
                            break;
                        case ID3v2Frames.ALBUM:
                            tagInfo.Album = data;
                            break;
                        case ID3v2Frames.TITLE:
                            tagInfo.Title = data;
                            break;
                        case ID3v1Frames.TRACK:
                            tagInfo.Track = data;
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

        public void Dispose()
        {
            if (_system != null)
            {
                _system.close();
                _system.release();

                _system = null;
            }
        }
    }
}
