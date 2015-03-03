using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using X2MP.Core;

namespace X2MP.Models
{
    class EqualizerViewModel
    {
        public ParamEqInfo[] EqualizerBands
        {
            get { return App.SoundEngine.EqualizerBands; }
        }


        public EqualizerViewModel()
        {
            foreach (var eq in EqualizerBands)
            {
                //for each band, hook up event when gain is adjusted
                eq.PropertyChanged += (sender, e) => {
                    App.SoundEngine.SetEqualizerBand(Array.IndexOf(EqualizerBands, sender), (sender as ParamEqInfo).Gain);
                };
            }
        }
    }
}
