using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using FooProject.Generator;
using static UnitTest.BigRleArrayTest;

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

    [BigRleArrayFlags("FooTestMarker2Collection")]
    [Flags]
    public enum TestMarker2
    {
        None = 0,
        Important = 1,
    }

    [BigRleArrayFlags(generatedClassName: "FooTestMarker3Collection")]
    [Flags]
    public enum TestMarker3
    {
        None = 0,
        Important = 1,
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

            var expected_list = new BigRleArrayRange<TestMarker>[] { 
                new BigRleArrayRange<TestMarker>(TestMarker.None, 0, 10), 
                new BigRleArrayRange<TestMarker>(TestMarker.Mark, 10, 500),
                new BigRleArrayRange<TestMarker>(TestMarker.None, 510, 490),
                new BigRleArrayRange<TestMarker>(TestMarker.Important, 1000, 100)
            };
            InterfaceTests.TestEnumerableElements<IRleArrayRange<TestMarker>>(collection.GetRanges(0,1100), expected_list);


            collection.Unset(10, 500, TestMarker.Mark);
            Assert.AreEqual(TestMarker.None, collection.Get(0));
            Assert.AreEqual(TestMarker.None, collection.Get(10));
            Assert.AreEqual(TestMarker.Important, collection.Get(1001));

            expected_list = new BigRleArrayRange<TestMarker>[] {
                new BigRleArrayRange<TestMarker>(TestMarker.None, 0, 10),
                new BigRleArrayRange<TestMarker>(TestMarker.None, 10, 500),
                new BigRleArrayRange<TestMarker>(TestMarker.None, 510, 490),
                new BigRleArrayRange<TestMarker>(TestMarker.Important, 1000, 100)
            };
            InterfaceTests.TestEnumerableElements<IRleArrayRange<TestMarker>>(collection.GetRanges(0, 1100), expected_list);

            FooTestMarker2Collection collection2 = new FooTestMarker2Collection();

            FooTestMarker3Collection collection3 = new FooTestMarker3Collection();
        }
    }
}
