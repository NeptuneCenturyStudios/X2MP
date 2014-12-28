using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X2MP.Core;
using System.Runtime.InteropServices;

namespace X2MP.Tests
{
    [TestClass]
    public class SoundEngineTests
    {
        [TestMethod]
        public void TestSoundEngineCreation()
        {
            //create instance
            var se = new SoundEngine();
            //dispose
            se.Dispose();
        }

        [TestMethod]
        public void TestPtrToStructure()
        {
            for (int x = 0; x < 10000000; x++)
            {
                var control = new SoundSystemControl();
                control.IsPlaying = true;
                var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(control));

                Marshal.StructureToPtr(control, ptr, true);

                //get
                SoundSystemControl s = (SoundSystemControl)Marshal.PtrToStructure(ptr, typeof(SoundSystemControl));

                Marshal.FreeHGlobal(ptr);
            }

        }
    }
}
