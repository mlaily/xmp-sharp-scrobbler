using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestConsole.UnitTestable;

namespace UnitTests
{
    [TestClass]
    public class General
    {
        [TestMethod]
        public void CheckApiFormat()
        {
            Misc.CheckApiFormat();
        }
    }
}
