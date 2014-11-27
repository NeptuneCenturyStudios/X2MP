using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using X2MP.Core;

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
    }
}
