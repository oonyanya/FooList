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

        class ReadOnlyList<T> : IComposableList<T>
        {
            int start, length;

            List<T> items;

            public T this[int index] { get => items[index]; set => throw new NotImplementedException(); }

            public int Count => this.length;

            public bool IsReadOnly => true;

            public ReadOnlyList(IEnumerable<T> collection)
            {
                if (collection != null)
                {
                    var readonlyList = collection as ReadOnlyList<T>;
                    if (readonlyList != null)
                    {
                        this.items = readonlyList.items;
                        this.start = readonlyList.start;
                        this.length = readonlyList.length;
                    }
                    else
                    {
                        items = new List<T>(collection);
                        this.start = 0;
                        this.length = items.Count;
                    }
                }
                else
                {
                    items = new List<T>();
                }
            }

            internal ReadOnlyList(ReadOnlyList<T> collection, int start = 0, int length = int.MaxValue)
            {
                this.items = collection.items;
                this.start = start;
                this.length = length == int.MaxValue ? collection.Count : length;
            }

            public void Add(T item)
            {
                throw new NotImplementedException();
            }

            public void AddRange(IEnumerable<T> collection, int collection_length = -1)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(T item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                //VisualStudioでのデバック用に最低限実装しないといけない
                this.items.CopyTo(this.start, array, arrayIndex, this.length);
            }

            public IEnumerator<T> GetEnumerator()
            {
                int start = this.start;
                int end = this.start + this.length - 1;
                for (int i = start; i <= end; i++)
                {
                    yield return items[i];
                }
            }

            public IEnumerable<T> GetRange(int index, int count)
            {
                return new ReadOnlyList<T>(this, index, count);
            }

            public int IndexOf(T item)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, T item)
            {
                throw new NotImplementedException();
            }

            public void InsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
            {
                throw new NotImplementedException();
            }

            public bool QueryAddRange(IEnumerable<T> collection, int collection_length = -1)
            {
                return false;
            }

            public bool QueryInsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
            {
                return false;
            }

            public bool QueryRemoveRange(int index, int count)
            {
                return false;
            }

            public bool Remove(T item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotImplementedException();
            }

            public void RemoveRange(int index, int count)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        class MixedCustomConverter<T> : DefaultCustomConverter<T>
        {
            public override IComposableList<T> CreateList(long init_capacity, long maxcapacity, IEnumerable<T> collection = null)
            {
                if (collection is ReadOnlyList<T>)
                {
                    var list = new ReadOnlyList<T>(collection);
                    return list;
                }
                else
                {
                    return base.CreateList(init_capacity, maxcapacity, collection);
                }
            }
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyList<char>("this is a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.RemoveAt(5);
            Assert.AreEqual("this s a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void RemoveRangeTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyList<char>("this is a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.RemoveRange(5, 2);
            Assert.AreEqual("this  a pen", new string(buf.ToArray()));
        }

        [TestMethod]
        public void InsertTest()
        {
            var customBuilder = new MixedCustomConverter<char>();
            customBuilder.DataStore = new MemoryPinableContentDataStore<IComposableList<char>>();
            var init_collection = new ReadOnlyList<char>("this  a pen");
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
            var init_collection = new ReadOnlyList<char>("this  a pen");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.InsertRange(5, "is");
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
            var init_collection = new ReadOnlyList<char>("this is a");
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
            var init_collection = new ReadOnlyList<char>("this is a");
            var buf = new FooProject.Collection.BigList<char>(init_collection, customBuilder, customBuilder);
            buf.CustomBuilder = customBuilder;
            buf.AddToFront(' ');
            Assert.AreEqual(" this is a", new string(buf.ToArray()));
        }
    }
}
