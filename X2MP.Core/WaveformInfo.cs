using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X2MP.Core
{
    /// <summary>
    /// Contains info about the waveform data retrieved from the waveform DSP unit
    /// </summary>
    public class WaveformInfo
    {
        public IntPtr Buffer { get; set; }
        public uint Length { get; set; }
        public int NumChannels { get; set; }
    }
}
