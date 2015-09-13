using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class UtilTests
    {
        [TestMethod]
        public void PartitionEnumerableExtension()
        {
            var sequence = Enumerable.Range(0, 500);
            var partitionned = xmp_sharp_scrobbler_managed.Util.Partition(sequence, 50);

            Assert.AreEqual(10, partitionned.Count());

            Assert.AreEqual(500, partitionned.SelectMany(x => x).Count());

            int i = 0;
            foreach (var partition in partitionned)
            {
                foreach (var item in partition)
                {
                    Assert.AreEqual(i, item);
                    i++;
                }
            }
        }
    }
}
