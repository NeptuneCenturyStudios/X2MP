using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace X2MP.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class SoundSystemControl
    {
        private byte _isPlaying;
        public bool IsPlaying
        {
            get
            {
                return (_isPlaying != 0 ? true : false);
            }
            set
            {
                _isPlaying = (byte)(value == true ? 1 : 0);
            }
        }
    }

}
