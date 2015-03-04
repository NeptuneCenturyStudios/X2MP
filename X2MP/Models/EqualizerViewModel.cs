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
        #region Properties

        public ParamEqInfo[] EqualizerBands
        {
            get { return App.SoundEngine.EqualizerBands; }
        }

        #region Commands

        public Command ResetEq { get; private set; }

        public Command RaiseGain { get; private set; }

        public Command LowerGain { get; private set; }

        #endregion
        #endregion

        #region Constructor
        public EqualizerViewModel()
        {
            //register the commands
            RegisterCommands();

            foreach (var eq in EqualizerBands)
            {
                //for each band, hook up event when gain is adjusted
                eq.PropertyChanged += (sender, e) =>
                {
                    App.SoundEngine.SetEqualizerBand(Array.IndexOf(EqualizerBands, sender), (sender as ParamEqInfo).Gain);
                };
            }
        }
        #endregion
        
        #region Register Commands
        private void RegisterCommands()
        {
            //reset
            ResetEq = new Command((parameter) =>
            {
                //clear
                foreach (var eqInfo in EqualizerBands)
                {
                    //fmod docs indicate that a value of 1.0 represents original,
                    //unmodified sound
                    eqInfo.Gain = 1.0f;
                }
            });

            //raise
            RaiseGain = new Command((parameter) =>
            {

                foreach (var eqInfo in EqualizerBands)
                {
                    //if any eq bands would go out of range, stop
                    if (eqInfo.Gain + 1.0f > eqInfo.Max)
                    {
                        return;
                    }
                }

                //clear
                foreach (var eqInfo in EqualizerBands)
                {
                    //fmod docs indicate that a value of 1.0 represents original,
                    //unmodified sound
                    eqInfo.Gain += 1.0f;
                }
            });

            //lower
            LowerGain = new Command((parameter) =>
            {

                foreach (var eqInfo in EqualizerBands)
                {
                    //if any eq bands would go out of range, stop
                    if (eqInfo.Gain - 1.0f < eqInfo.Min)
                    {
                        return;
                    }
                }

                //clear
                foreach (var eqInfo in EqualizerBands)
                {
                    //fmod docs indicate that a value of 1.0 represents original,
                    //unmodified sound
                    eqInfo.Gain -= 1.0f;
                }
            });
        }
        #endregion
    }
}
