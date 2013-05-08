using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ctr.FormatLibrary.Pwx;

namespace Ctr.FormatLibraryTests
{
    [TestClass]
    public class PwxConverterTests
    {
        [DeploymentItem(@"SampleFiles\gear_2008-11-03-01-40-21Z_0.pwx")]
        [TestMethod]
        public void ConvertToGpx()
        {
            PwxConverter converter = new PwxConverter("gear_2008-11-03-01-40-21Z_0.pwx");

            converter.SaveAsGpx("gear_2008-11-03-01-40-21Z_0.pwx" + ".gpx");
        }

        [DeploymentItem(@"SampleFiles\gear_2008-11-03-01-40-21Z_0.pwx")]
        [TestMethod]
        public void ConvertToTcx()
        {
            PwxConverter converter = new PwxConverter("gear_2008-11-03-01-40-21Z_0.pwx");

            converter.SaveAsTcx("gear_2008-11-03-01-40-21Z_0.pwx" + ".tcx");
        }

        [DeploymentItem(@"SampleFiles\test1.pwx")]
        [TestMethod]
        public void ConvertToTcx2()
        {
            PwxConverter converter = new PwxConverter("test1.pwx");

            converter.SaveAsTcx("test1.pwx" + ".tcx");
        }

        [DeploymentItem(@"SampleFiles\has power.pwx")]
        [TestMethod]
        public void ConvertToTcx3()
        {
            PwxConverter converter = new PwxConverter("has power.pwx");

            converter.SaveAsTcx("has power.pwx" + ".tcx");
        }
    }
}
