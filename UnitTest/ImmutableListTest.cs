using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace UnitTest
{
    [TestClass]
    public sealed class ImmutableListTest
    {

        class MixedCustomConverter<T> : DefaultCustomConverter<T>
        {
            public override IComposableList<T> CreateList(long init_capacity, long maxcapacity, IEnumerable<T> collection = null)
            {
                var list = new ReadOnlyComposableList<T>(collection);
                return list;
            }
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this is a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.RemoveAt(5);
            Assert.AreEqual("this s a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void RemoveRangeTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this is a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.RemoveRange(5, 2);
            Assert.AreEqual("this  a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void InsertTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this  a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.Insert(5, 'i');
            Assert.AreEqual("this i a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void InsertRangeTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this  a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.InsertRange(5, "is");
            Assert.AreEqual("this is a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void SetTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this xs a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf[5] = 'i';
            Assert.AreEqual("this is a pen", new string(buf.ToArray()));

        }

        [TestMethod]
        public void AddTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var buf = new FooProject.Collection.BigList<char>("this is a", customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.Add(' ');
            Assert.AreEqual("this is a ", new string(buf.ToArray()));
        }

        [TestMethod]
        public void AddRangeTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this is a");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.AddRange(" pen");
            Assert.AreEqual("this is a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void AddFrontTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyComposableList<char>("this is a");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.AddToFront(' ');
            Assert.AreEqual(" this is a", new string(buf.ToArray()));
        }
    }
}
