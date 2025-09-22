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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FooProject.Collection;

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
