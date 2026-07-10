using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using FooProject.Generator;

namespace UnitTest
{
    [BigRleArrayFlags]
    [Flags]
    public enum TestMarker
    {
        None = 0,
        Important = 1,
        Mark = 2,
    }

    [TestClass]
    public class EnumWithFlagsGeneratorTest
    {
        [TestMethod]
        public void GenerateTest()
        {
            var collection = new TestMarkerCollection();

            collection.Add(TestMarker.None, 1000);
            collection.Add(TestMarker.Important, 100);
            Assert.AreEqual(TestMarker.None, collection.Get(0));
            Assert.AreEqual(TestMarker.Important, collection.Get(1001));

            collection.Set(10, 500, TestMarker.Mark);
            Assert.AreEqual(TestMarker.None, collection.Get(0));
            Assert.AreEqual(TestMarker.Important, collection.Get(1001));
            Assert.AreEqual(TestMarker.Mark, collection.Get(10));

            collection.Unset(10, 500, TestMarker.Mark);
            Assert.AreEqual(TestMarker.None, collection.Get(0));
            Assert.AreEqual(TestMarker.None, collection.Get(10));
            Assert.AreEqual(TestMarker.Important, collection.Get(1001));
        }
    }
}
