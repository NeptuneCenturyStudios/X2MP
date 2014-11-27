using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using X2MP.Core;

namespace X2MP
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets the static reference to the sound engine (FMOD)
        /// </summary>
        public static SoundEngine SoundEngine { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public App()
        {
            //initialize instance of FMOD engine
            SoundEngine = new SoundEngine();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //shutdown fmod
            SoundEngine.Dispose();

            base.OnExit(e);
        }
    }
}
