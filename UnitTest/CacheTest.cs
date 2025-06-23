using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection.DataStore;

namespace UnitTest
{
    [TestClass]
    public class CacheTest
    {
        void SetTestCore(ICacheList<int,char> cache)
        {
            int overflowcount = 0;
            var test_collection = Enumerable.Range(1, cache.Limit * 2);
            foreach (var item in test_collection)
            {
                if(cache.Set(item, 'a'))
                    overflowcount++;
            }
            Assert.IsTrue(overflowcount > 0);

            char overflowitem;
            bool result = cache.Set(cache.Limit * 2 + 1, 'a', out overflowitem);
            Assert.AreEqual(true, result);
            Assert.AreEqual('a', overflowitem);
        }

        [TestMethod]
        public void SetTest()
        {
            bool overflowed = false;
            ICacheList<int, char> cache;
            cache = new FIFOCacheList<int, char>();
            cache.CacheOuted += (e) =>
            {
                overflowed = true;
            };
            SetTestCore(cache);
            Assert.AreEqual(true, overflowed);

            overflowed = false;
            cache = new TwoQueueCacheList<int, char>();
            cache.CacheOuted += (e) =>
            {
                overflowed = true;
            };
            SetTestCore(cache);
            Assert.AreEqual(true, overflowed);
        }

        void GetTestCore(ICacheList<int, char> cache)
        {
            var test_collection = Enumerable.Range(1, cache.Limit + 1);
            foreach (var item in test_collection)
            {
                cache.Set(item, 'a');
            }
            char got_item;
            bool result;
            result = cache.TryGet(cache.Limit, out got_item);
            Assert.AreEqual('a', got_item);
            Assert.AreEqual(true, result);

            result = cache.TryGet(cache.Limit + 2, out got_item);
            Assert.AreEqual(default(char), got_item);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void GetTest()
        {
            ICacheList<int, char> cache;
            cache = new FIFOCacheList<int, char>();
            GetTestCore(cache);
            cache = new TwoQueueCacheList<int, char>();
            GetTestCore(cache);
        }

        void ForEachValueTestCore(ICacheList<int, char> cache)
        {
            var test_collection = Enumerable.Range(1, cache.Limit + 1);
            foreach (var item in test_collection)
            {
                cache.Set(item, 'a');
            }
            foreach(var item in cache.ForEachValue())
            {
                Assert.AreEqual('a', item);
            }
        }

        [TestMethod]
        public void ForEachValueTest()
        {
            ICacheList<int, char> cache;
            cache = new FIFOCacheList<int, char>();
            ForEachValueTestCore(cache);
            cache = new TwoQueueCacheList<int, char>();
            ForEachValueTestCore(cache);
        }

        void FlushTestCore(ICacheList<int, char> cache)
        {
            var test_collection = Enumerable.Range(1, cache.Limit);
            foreach (var item in test_collection)
            {
                cache.Set(item, 'a');
            }
            cache.Flush();

            char got_item;
            bool result;
            result = cache.TryGet(1, out got_item);
            Assert.AreEqual(default(char), got_item);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void FlushTest()
        {
            ICacheList<int, char> cache;

            bool overflowed = false;
            cache = new FIFOCacheList<int, char>();
            cache.CacheOuted += (e) =>
            {
                overflowed = true;
            };
            FlushTestCore(cache);
            Assert.AreEqual(true, overflowed);

            overflowed = false;
            cache = new TwoQueueCacheList<int, char>();
            cache.CacheOuted += (e) =>
            {
                overflowed = true;
            };
            FlushTestCore(cache);
            Assert.AreEqual(true, overflowed);
        }
    }
}
