using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X2MP.Core
{
    class MediaInfo
    {
        /// <summary>
        /// Gets or sets the filename of the media
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets the open state of the media
        /// </summary>
        public FMOD.OPENSTATE OpenState { get; private set; }

        /// <summary>
        /// Gets or the setst
        /// </summary>
        public FMOD.Sound Sound = null;

        /// <summary>
        /// Check the open state of the media
        /// </summary>
        public void UpdateOpenState()
        {
            if (Sound != null)
            {
                FMOD.OPENSTATE openState;
                uint percentBuffered;
                bool starving;
                bool diskBusy;

                //check to see if it is ready
                Sound.getOpenState(out openState, out percentBuffered, out starving, out diskBusy);

                //set the open state
                OpenState = openState;
            }
        }
    }
}
