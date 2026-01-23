/*
 *  Copy from
 *  https://github.com/timdetering/Wintellect.PowerCollections
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using FooProject.Collection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    /// <summary>
    /// The BinaryPredicate delegate type  encapsulates a method that takes two
    /// items of the same type, and returns a boolean value representating 
    /// some relationship between them. For example, checking whether two
    /// items are equal or equivalent is one kind of binary predicate.
    /// </summary>
    /// <param name="item1">The first item.</param>
    /// <param name="item2">The second item.</param>
    /// <returns>Whether item1 and item2 satisfy the relationship that the BinaryPredicate defines.</returns>
    public delegate bool BinaryPredicate<T>(T item1, T item2);
    
    /// <summary>
    /// A collection of generic tests for various interfaces.
    /// </summary>
    internal static class InterfaceTests
    {
        /// <summary>
        /// Test an IEnumerable should contain the given values in order
        /// </summary>
        public static void TestEnumerableElements(IEnumerable<char> e, StringBuilder expected, BinaryPredicate<char> equals = null)
        {
            if (equals == null)
                equals = delegate (char x, char y) { return object.Equals(x, y); };

            int i = 0;
            foreach (var item in e)
            {
                Assert.IsTrue(equals(expected[i], item));
                ++i;
            }
            Assert.AreEqual(expected.Length, i);
        }

        /// <summary>
        /// Test an IEnumerable should contain the given values in order
        /// </summary>
        public static void TestIndexerElements(IList<char> e, StringBuilder expected, BinaryPredicate<char> equals = null)
        {
            if (equals == null)
                equals = delegate (char x, char y) { return object.Equals(x, y); };

            for (int i = 0; i < e.Count; ++i)
            {
                Assert.IsTrue(equals(expected[i], e[i]));
            }
        }

        /// <summary>
        /// Test an IEnumerable should contain the given values in order
        /// </summary>
        public static void TestEnumerableElements<T>(IEnumerable<T> e, T[] expected)
        {
            TestEnumerableElements<T>(e, expected, null);
        }

        public static void TestEnumerableElements<T>(IEnumerable<T> e, T[] expected, BinaryPredicate<T> equals)
        {
            if (equals == null)
                equals = delegate (T x, T y) { return object.Equals(x, y); };

            int i = 0;
            foreach (T item in e)
            {
                Assert.IsTrue(equals(expected[i], item));
                ++i;
            }
            Assert.AreEqual(expected.Length, i);
        }

        // Check collection read-only exceptions
        private static void CheckReadonlyCollectionException(Exception e, string name)
        {
            Assert.IsTrue(e is NotSupportedException);
        }

        /// <summary>
		///  Test a readonly ICollection&lt;string&gt; that should contain the given values, possibly in order. Checks only the following items:
		///     GetEnumerator, CopyTo, Count, Contains, IsReadOnly
		/// </summary>
		/// <param name="coll">ICollection&lt;T&gt; to test. </param>
		/// <param name="valueArray">The values that should be in the collection.</param>
		/// <param name="mustBeInOrder">Must the value be in order?</param>
		/// <param name="name">Expected name of the collection, or null for don't check.</param>
        public static void TestReadonlyCollectionGeneric<T>(ReadOnlyBigList<T> coll, T[] valueArray, bool mustBeInOrder, string name)
        {
            TestReadonlyCollectionGeneric<T>(coll, valueArray, mustBeInOrder, null, null);
        }

        public static void TestReadonlyCollectionGeneric<T>(ReadOnlyBigList<T> coll, T[] valueArray, bool mustBeInOrder, string name, BinaryPredicate<T> equals)
        {
            TestCollectionGeneric<T>(coll, valueArray, mustBeInOrder, equals);

            // Test read-only flag.
            Assert.IsTrue(coll.IsReadOnly);

            // Check that Clear throws correct exception
            if (coll.Count > 0)
            {
                try
                {
                    coll.Clear();
                    Assert.Fail("Should throw exception");
                }
                catch (Exception e)
                {
                    CheckReadonlyCollectionException(e, name);
                }
            }

            // Check that Add throws correct exception
            try
            {
                coll.Add(default(T));
                Assert.Fail("Should throw exception");
            }
            catch (Exception e)
            {
                CheckReadonlyCollectionException(e, name);
            }

            // Check throws correct exception
            try
            {
                coll.Remove(default(T));
                Assert.Fail("Should throw exception");
            }
            catch (Exception e)
            {
                CheckReadonlyCollectionException(e, name);
            }

        }

        /// <summary>
                 /// Test read-only IList&lt;T&gt; that should contain the given values, possibly in order. Does not change
                 /// the list. Forces the list to be read-only.
                 /// </summary>
                 /// <typeparam name="T"></typeparam>
                 /// <param name="coll">IList&lt;T&gt; to test. </param>
                 /// <param name="valueArray">The values that should be in the list.</param>
                 /// <param name="name">Name of the collection, for exceptions. Null to not check.</param>
        public static void TestReadOnlyListGeneric<T>(ReadOnlyBigList<T> coll, T[] valueArray, string name)
        {
            TestReadOnlyListGeneric<T>(coll, valueArray, name, null);
        }

        public static void TestReadOnlyListGeneric<T>(ReadOnlyBigList<T> coll, T[] valueArray, string name, BinaryPredicate<T> equals)
        {
            if (equals == null)
                equals = delegate (T x, T y) { return object.Equals(x, y); };

            // Basic list stuff.
            TestListGeneric<T>(coll, valueArray, equals);
            TestReadonlyCollectionGeneric<T>(coll, valueArray, true, name, equals);

            // Check read only and fixed size bits.
            Assert.IsTrue(coll.IsReadOnly);

            // Check exceptions.
            if (coll.Count > 0)
            {
                try
                {
                    coll.Clear();
                    Assert.Fail("Should throw exception");
                }
                catch (Exception e)
                {
                    CheckReadonlyCollectionException(e, name);
                }
            }

            try
            {
                coll.Insert(0, default(T));
                Assert.Fail("Should throw exception");
            }
            catch (Exception e)
            {
                CheckReadonlyCollectionException(e, name);
            }

            if (coll.Count > 0)
            {
                try
                {
                    coll.RemoveAt(0);
                    Assert.Fail("Should throw exception");
                }
                catch (Exception e)
                {
                    CheckReadonlyCollectionException(e, name);
                }

                try
                {
                    coll[0] = default(T);
                    Assert.Fail("Should throw exception");
                }
                catch (Exception e)
                {
                    CheckReadonlyCollectionException(e, name);
                }
            }
        }

        /// /// <summary>
		///  Test an ICollection&lt;string&gt; that should contain the given values, possibly in order. Checks only the following items:
		///     GetEnumerator, CopyTo, Count, Contains
		/// </summary>
		/// <param name="coll">ICollection to test. </param>
		/// <param name="valueArray">The elements that should be in the collection.</param>
		/// <param name="mustBeInOrder">Must the elements be in order?</param>
        /// <param name="equals">Predicate to test for equality; null for default.</param>
		private static void TestCollectionGeneric<T>(ICollection<T> coll, T[] values, bool mustBeInOrder, BinaryPredicate<T> equals)
        {
            if (equals == null)
                equals = delegate (T x, T y) { return object.Equals(x, y); };

            bool[] used = new bool[values.Length];

            // Check ICollection.Count.
            Assert.AreEqual(values.Length, coll.Count);

            // Check ICollection.GetEnumerator().
            int i = 0, j;

            foreach (T s in coll)
            {
                if (mustBeInOrder)
                {
                    Assert.IsTrue(equals(values[i], s));
                }
                else
                {
                    bool found = false;

                    for (j = 0; j < values.Length; ++j)
                    {
                        if (!used[j] && equals(values[j], s))
                        {
                            found = true;
                            used[j] = true;
                            break;
                        }
                    }

                    Assert.IsTrue(found);
                }

                ++i;
            }

            // Check Contains
            foreach (T s in values)
            {
                Assert.IsTrue(coll.Contains(s));
            }

            // Check CopyTo.
            used = new bool[values.Length];

            T[] newKeys = new T[coll.Count + 2];

            coll.CopyTo(newKeys, 1);
            for (i = 0, j = 1; i < coll.Count; ++i, ++j)
            {
                if (mustBeInOrder)
                {
                    Assert.IsTrue(equals(values[i], newKeys[j]));
                }
                else
                {
                    bool found = false;

                    for (int k = 0; k < values.Length; ++k)
                    {
                        if (!used[k] && equals(values[k], newKeys[j]))
                        {
                            found = true;
                            used[k] = true;
                            break;
                        }
                    }

                    Assert.IsTrue(found);
                }
            }

            // Shouldn't have distubed the values around what was filled in.
            Assert.IsTrue(equals(default(T), newKeys[0]));
            Assert.IsTrue(equals(default(T), newKeys[coll.Count + 1]));

            if (coll.Count != 0)
            {
                // Check CopyTo exceptions.
                try
                {
                    coll.CopyTo(null, 0);
                    Assert.Fail("Copy to null should throw exception");
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is ArgumentNullException);
                }
                try
                {
                    coll.CopyTo(newKeys, 3);
                    Assert.Fail("CopyTo should throw argument exception");
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is ArgumentException);
                }
                try
                {
                    coll.CopyTo(newKeys, -1);
                    Assert.Fail("CopyTo should throw argument out of range exception");
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is ArgumentOutOfRangeException);
                }
            }
        }

        /// <summary>
        ///  Test a read-write IList&lt;T&gt; that should contain the given values, possibly in order. Destroys the collection in the process.
        /// </summary>
        /// <param name="coll">IList&lt;T&gt; to test. </param>
        /// <param name="valueArray">The values that should be in the list.</param>
        public static void TestReadWriteListGeneric<T>(IList<T> coll, T[] valueArray)
        {
            TestReadWriteListGeneric<T>(coll, valueArray, null);
        }

        public static void TestReadWriteListGeneric<T>(IList<T> coll, T[] valueArray, BinaryPredicate<T> equals)
        {
            if (equals == null)
                equals = delegate (T x, T y) { return object.Equals(x, y); };

            TestListGeneric(coll, valueArray, equals);     // Basic read-only list stuff.

            // Check the indexer getter.
            T[] save = new T[coll.Count];
            for (int i = coll.Count - 1; i >= 0; --i)
            {
                Assert.AreEqual(valueArray[i], coll[i]);
                int index = coll.IndexOf(valueArray[i]);
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && object.Equals(coll[index], valueArray[i])));
                save[i] = coll[i];
            }

            // Check the setter by reversing the list.
            for (int i = 0; i < coll.Count / 2; ++i)
            {
                T temp = coll[i];
                coll[i] = coll[coll.Count - 1 - i];
                coll[coll.Count - 1 - i] = temp;
            }

            for (int i = 0; i < coll.Count; ++i)
            {
                Assert.AreEqual(valueArray[coll.Count - 1 - i], coll[i]);
                int index = coll.IndexOf(valueArray[coll.Count - 1 - i]);
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && object.Equals(coll[index], valueArray[coll.Count - 1 - i])));
            }

            // Reverse back
            for (int i = 0; i < coll.Count / 2; ++i)
            {
                T temp = coll[i];
                coll[i] = coll[coll.Count - 1 - i];
                coll[coll.Count - 1 - i] = temp;
            }

            T item = valueArray.Length > 0 ? valueArray[valueArray.Length / 2] : default(T);
            // Check exceptions from index out of range.
            try
            {
                coll[-1] = item;
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll[int.MinValue] = item;
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                T dummy = coll[-2];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll[coll.Count] = item;
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                T dummy = coll[coll.Count];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                T dummy = coll[int.MaxValue];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll[int.MaxValue] = item;
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.Insert(-1, item);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.Insert(coll.Count + 1, item);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.Insert(int.MaxValue, item);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.RemoveAt(coll.Count);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.RemoveAt(-1);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.RemoveAt(int.MaxValue);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.RemoveAt(coll.Count);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            // Insert at the beginning.
            coll.Insert(0, item);
            Assert.AreEqual(coll[0], item);
            Assert.AreEqual(valueArray.Length + 1, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i + 1]);

            // Insert at the end
            coll.Insert(valueArray.Length + 1, item);
            Assert.AreEqual(coll[valueArray.Length + 1], item);
            Assert.AreEqual(valueArray.Length + 2, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i + 1]);

            // Delete at the beginning.
            coll.RemoveAt(0);
            Assert.AreEqual(coll[valueArray.Length], item);
            Assert.AreEqual(valueArray.Length + 1, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i]);

            // Delete at the end.
            coll.RemoveAt(valueArray.Length);
            Assert.AreEqual(valueArray.Length, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i]);

            // Insert at the middle.
            coll.Insert(valueArray.Length / 2, item);
            Assert.AreEqual(valueArray.Length + 1, coll.Count);
            Assert.AreEqual(item, coll[valueArray.Length / 2]);
            for (int i = 0; i < valueArray.Length; ++i)
            {
                if (i < valueArray.Length / 2)
                    Assert.AreEqual(valueArray[i], coll[i]);
                else
                    Assert.AreEqual(valueArray[i], coll[i + 1]);
            }

            // Delete at the middle.
            coll.RemoveAt(valueArray.Length / 2);
            Assert.AreEqual(valueArray.Length, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i]);

            // Delete all from the middle.
            for (int i = 0; i < valueArray.Length; ++i)
                coll.RemoveAt(coll.Count / 2);
            Assert.AreEqual(0, coll.Count);

            // Build up in order.
            for (int i = 0; i < save.Length; ++i)
            {
                coll.Insert(i, save[i]);
            }

            TestListGeneric(coll, valueArray, equals);     // Basic read-only list stuff.

            coll.Clear();
            Assert.AreEqual(0, coll.Count);

            // Build up in reverse order.
            for (int i = 0; i < save.Length; ++i)
            {
                coll.Insert(0, save[save.Length - 1 - i]);
            }
            TestListGeneric(coll, valueArray, equals);     // Basic read-only list stuff.

            // Check read-write collection stuff.
            TestReadWriteCollectionGeneric<T>(coll, valueArray, true);
        }

        /// <summary>
        ///  Test a read-write ICollection&lt;string&gt; that should contain the given values, possibly in order. Destroys the collection in the process.
        /// </summary>
        /// <param name="coll">ICollection to test. </param>
        /// <param name="valueArray">The values that should be in the collection.</param>
        /// <param name="mustBeInOrder">Must the values be in order?</param>
        public static void TestReadWriteCollectionGeneric<T>(ICollection<T> coll, T[] valueArray, bool mustBeInOrder)
        {
            TestReadWriteCollectionGeneric<T>(coll, valueArray, mustBeInOrder, null);
        }

        public static void TestReadWriteCollectionGeneric<T>(ICollection<T> coll, T[] valueArray, bool mustBeInOrder, BinaryPredicate<T> equals)
        {
            TestCollectionGeneric<T>(coll, valueArray, mustBeInOrder, equals);

            // Test read-only flag.
            Assert.IsFalse(coll.IsReadOnly);

            // Clear and Count.
            coll.Clear();
            Assert.AreEqual(0, coll.Count);

            // Add all the items back.
            foreach (T item in valueArray)
                coll.Add(item);
            Assert.AreEqual(valueArray.Length, coll.Count);
            TestCollectionGeneric<T>(coll, valueArray, mustBeInOrder, equals);

            // Remove all the items again.
            foreach (T item in valueArray)
                coll.Remove(item);
            Assert.AreEqual(0, coll.Count);
        }

        /// <summary>
        ///  Test an ICollection that should contain the given values, possibly in order.
        /// </summary>
        /// <param name="coll">ICollection to test. </param>
        /// <param name="valueArray">The values that should be in the collection.</param>
        /// <param name="mustBeInOrder">Must the values be in order?</param>
        public static void TestCollection<T>(ICollection coll, T[] valueArray, bool mustBeInOrder)
        {
            T[] values = (T[])valueArray.Clone();       // clone the array so we can destroy it.

            // Check ICollection.Count.
            Assert.AreEqual(values.Length, coll.Count);

            // Check ICollection.GetEnumerator().
            int i = 0, j;

            foreach (T s in coll)
            {
                if (mustBeInOrder)
                {
                    Assert.AreEqual(values[i], s);
                }
                else
                {
                    bool found = false;

                    for (j = 0; j < values.Length; ++j)
                    {
                        if (object.Equals(values[j], s))
                        {
                            found = true;
                            values[j] = default(T);
                            break;
                        }
                    }

                    Assert.IsTrue(found);
                }

                ++i;
            }

            // Check IsSyncronized, SyncRoot.
            Assert.IsFalse(coll.IsSynchronized);
            Assert.IsNotNull(coll.SyncRoot);

            // Check CopyTo.
            values = (T[])valueArray.Clone();       // clone the array so we can destroy it.

            T[] newKeys = new T[coll.Count + 2];

            coll.CopyTo(newKeys, 1);
            for (i = 0, j = 1; i < coll.Count; ++i, ++j)
            {
                if (mustBeInOrder)
                {
                    Assert.AreEqual(values[i], newKeys[j]);
                }
                else
                {
                    bool found = false;

                    for (int k = 0; k < values.Length; ++k)
                    {
                        if (object.Equals(values[k], newKeys[j]))
                        {
                            found = true;
                            values[k] = default(T);
                            break;
                        }
                    }

                    Assert.IsTrue(found);
                }
            }

            // Shouldn't have disturbed the values around what was filled in.
            Assert.AreEqual(default(T), newKeys[0]);
            Assert.AreEqual(default(T), newKeys[coll.Count + 1]);

            // Check CopyTo exceptions.
            if (coll.Count > 0)
            {
                try
                {
                    coll.CopyTo(null, 0);
                    Assert.Fail("Copy to null should throw exception");
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is ArgumentNullException);
                }
                try
                {
                    coll.CopyTo(newKeys, 3);
                    Assert.Fail("CopyTo should throw argument exception");
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is ArgumentException);
                }
                try
                {
                    coll.CopyTo(newKeys, -1);
                    Assert.Fail("CopyTo should throw argument out of range exception");
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is ArgumentOutOfRangeException);
                }
            }
        }
        
        /// <summary>
                 /// Test read-only non-generic IList that should contain the given values, possibly in order. Does not change
                 /// the list. Does not force the list to be read-only.
                 /// </summary>
                 /// <param name="coll">IList to test. </param>
                 /// <param name="valueArray">The values that should be in the list.</param>
        public static void TestList<T>(IList coll, T[] valueArray)
        {
            // Check basic read-only collection stuff.
            TestCollection<T>(coll, valueArray, true);

            // Check the indexer getter and IndexOf, backwards
            for (int i = coll.Count - 1; i >= 0; --i)
            {
                Assert.AreEqual(valueArray[i], coll[i]);
                int index = coll.IndexOf(valueArray[i]);
                Assert.IsTrue(coll.Contains(valueArray[i]));
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && object.Equals(coll[index], valueArray[i])));
            }

            // Check the indexer getter and IndexOf, forwards
            for (int i = 0; i < valueArray.Length; ++i)
            {
                Assert.AreEqual(valueArray[i], coll[i]);
                int index = coll.IndexOf(valueArray[i]);
                Assert.IsTrue(coll.Contains(valueArray[i]));
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && object.Equals(coll[index], valueArray[i])));
            }

            // Check the indexer getter and IndexOf, jumping by 3s
            for (int i = 0; i < valueArray.Length; i += 3)
            {
                Assert.AreEqual(valueArray[i], coll[i]);
                int index = coll.IndexOf(valueArray[i]);
                Assert.IsTrue(coll.Contains(valueArray[i]));
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && object.Equals(coll[index], valueArray[i])));
            }

            // Check exceptions from index out of range.
            try
            {
                object dummy = coll[-1];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                object dummy = coll[int.MinValue];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                object dummy = coll[-2];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                object dummy = coll[coll.Count];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                object dummy = coll[int.MaxValue];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            // Check bad type.
            if (typeof(T) != typeof(object))
            {
                int index = coll.IndexOf(new object());
                Assert.AreEqual(-1, index);

                bool b = coll.Contains(new object());
                Assert.IsFalse(b);
            }
        }
        
        /// <summary>
                 ///  Test a read-write non-generic IList that should contain the given values, possibly in order. Destroys the collection in the process.
                 /// </summary>
                 /// <param name="coll">IList to test. </param>
                 /// <param name="valueArray">The values that should be in the list.</param>
        public static void TestReadWriteList<T>(IList coll, T[] valueArray)
        {
            TestList(coll, valueArray);     // Basic read-only list stuff.

            // Check read only
            Assert.IsFalse(coll.IsReadOnly);
            Assert.IsFalse(coll.IsReadOnly);

            // Check the indexer getter.
            T[] save = new T[coll.Count];
            for (int i = coll.Count - 1; i >= 0; --i)
            {
                Assert.AreEqual(valueArray[i], coll[i]);
                int index = coll.IndexOf(valueArray[i]);
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && object.Equals(coll[index], valueArray[i])));
                save[i] = (T)coll[i];
            }

            // Check the setter by reversing the list.
            for (int i = 0; i < coll.Count / 2; ++i)
            {
                T temp = (T)coll[i];
                coll[i] = coll[coll.Count - 1 - i];
                coll[coll.Count - 1 - i] = temp;
            }

            for (int i = 0; i < coll.Count; ++i)
            {
                Assert.AreEqual(valueArray[coll.Count - 1 - i], coll[i]);
                int index = coll.IndexOf(valueArray[coll.Count - 1 - i]);
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && object.Equals(coll[index], valueArray[coll.Count - 1 - i])));
            }

            // Reverse back
            for (int i = 0; i < coll.Count / 2; ++i)
            {
                T temp = (T)coll[i];
                coll[i] = coll[coll.Count - 1 - i];
                coll[coll.Count - 1 - i] = temp;
            }

            T item = valueArray.Length > 0 ? valueArray[valueArray.Length / 2] : default(T);
            // Check exceptions from index out of range.
            try
            {
                coll[-1] = item;
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll[int.MinValue] = item;
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                object dummy = coll[-2];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll[coll.Count] = item;
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                object dummy = coll[coll.Count];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                object dummy = coll[int.MaxValue];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll[int.MaxValue] = item;
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.Insert(-1, item);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.Insert(coll.Count + 1, item);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.Insert(int.MaxValue, item);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.RemoveAt(coll.Count);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.RemoveAt(-1);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.RemoveAt(int.MaxValue);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                coll.RemoveAt(coll.Count);
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

            // Check operations with bad type.
            if (typeof(T) != typeof(object))
            {
                try
                {
                    coll.Add(new object());
                    Assert.Fail("should throw");
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is ArgumentException);
                }

                try
                {
                    coll.Insert(0, new object());
                    Assert.Fail("should throw");
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is ArgumentException);
                }

                int index = coll.IndexOf(new object());
                Assert.AreEqual(-1, index);

                coll.Remove(new object());
            }

            // Insert at the beginning.
            coll.Insert(0, item);
            Assert.AreEqual(coll[0], item);
            Assert.AreEqual(valueArray.Length + 1, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i + 1]);

            // Insert at the end
            coll.Insert(valueArray.Length + 1, item);
            Assert.AreEqual(coll[valueArray.Length + 1], item);
            Assert.AreEqual(valueArray.Length + 2, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i + 1]);

            // Delete at the beginning.
            coll.RemoveAt(0);
            Assert.AreEqual(coll[valueArray.Length], item);
            Assert.AreEqual(valueArray.Length + 1, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i]);

            // Delete at the end.
            coll.RemoveAt(valueArray.Length);
            Assert.AreEqual(valueArray.Length, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i]);

            // Insert at the middle.
            coll.Insert(valueArray.Length / 2, item);
            Assert.AreEqual(valueArray.Length + 1, coll.Count);
            Assert.AreEqual(item, coll[valueArray.Length / 2]);
            for (int i = 0; i < valueArray.Length; ++i)
            {
                if (i < valueArray.Length / 2)
                    Assert.AreEqual(valueArray[i], coll[i]);
                else
                    Assert.AreEqual(valueArray[i], coll[i + 1]);
            }

            // Delete at the middle.
            coll.RemoveAt(valueArray.Length / 2);
            Assert.AreEqual(valueArray.Length, coll.Count);
            for (int i = 0; i < valueArray.Length; ++i)
                Assert.AreEqual(valueArray[i], coll[i]);

            // Delete all from the middle.
            for (int i = 0; i < valueArray.Length; ++i)
                coll.RemoveAt(coll.Count / 2);
            Assert.AreEqual(0, coll.Count);

            // Build up in order.
            for (int i = 0; i < save.Length; ++i)
            {
                coll.Insert(i, save[i]);
            }

            TestList<T>(coll, valueArray);     // Basic read-only list stuff.

            coll.Clear();
            Assert.AreEqual(0, coll.Count);

            // Build up in order with Add
            for (int i = 0; i < save.Length; ++i)
            {
                coll.Add(save[i]);
            }

            TestList<T>(coll, valueArray);     // Basic read-only list stuff.

            // Remove in order with Remove.
            for (int i = 0; i < valueArray.Length; ++i)
            {
                coll.Remove(valueArray[i]);
            }

            Assert.AreEqual(0, coll.Count);

            // Build up in reverse order with Insert
            for (int i = 0; i < save.Length; ++i)
            {
                coll.Insert(0, save[save.Length - 1 - i]);
            }
            TestList<T>(coll, valueArray);     // Basic read-only list stuff.

            // Check read-write collection stuff.
            TestCollection<T>(coll, valueArray, true);
        }
        /// <summary>
        /// Test read-only IList&lt;T&gt; that should contain the given values, possibly in order. Does not change
        /// the list. Does not force the list to be read-only.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="coll">IList&lt;T&gt; to test. </param>
        /// <param name="valueArray">The values that should be in the list.</param>
        public static void TestListGeneric<T>(IList<T> coll, T[] valueArray)
        {
            TestListGeneric<T>(coll, valueArray, null);
        }

        public static void TestListGeneric<T>(IList<T> coll, T[] valueArray, BinaryPredicate<T> equals)
        {
            if (equals == null)
                equals = delegate (T x, T y) { return object.Equals(x, y); };

            // Check basic read-only collection stuff.
            TestCollectionGeneric<T>(coll, valueArray, true, equals);

            // Check the indexer getter and IndexOf, backwards
            for (int i = coll.Count - 1; i >= 0; --i)
            {
                Assert.IsTrue(equals(valueArray[i], coll[i]));
                int index = coll.IndexOf(valueArray[i]);
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && equals(coll[index], valueArray[i])));
            }

            // Check the indexer getter and IndexOf, forwards
            for (int i = 0; i < valueArray.Length; ++i)
            {
                Assert.IsTrue(equals(valueArray[i], coll[i]));
                int index = coll.IndexOf(valueArray[i]);
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && equals(coll[index], valueArray[i])));
            }

            // Check the indexer getter and IndexOf, jumping by 3s
            for (int i = 0; i < valueArray.Length; i += 3)
            {
                Assert.IsTrue(equals(valueArray[i], coll[i]));
                int index = coll.IndexOf(valueArray[i]);
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index == i || (index < i && equals(coll[index], valueArray[i])));
            }

            // Check exceptions from index out of range.
            try
            {
                T dummy = coll[-1];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                T dummy = coll[int.MinValue];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                T dummy = coll[-2];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                T dummy = coll[coll.Count];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }
            try
            {
                T dummy = coll[int.MaxValue];
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is ArgumentOutOfRangeException);
            }

        }
    }
}
