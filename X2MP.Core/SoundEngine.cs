using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace X2MP.Core
{
    public class SoundEngine : IDisposable
    {

        FMOD.System _system;


        /// <summary>
        /// Start the FMOD sound system engine
        /// </summary>
        public SoundEngine()
        {
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
