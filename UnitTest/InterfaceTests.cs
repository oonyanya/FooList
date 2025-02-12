//******************************
// Written by Peter Golde
// Copyright (c) 2004-2007, Wintellect
//
// Use and restribution of this code is subject to the license agreement 
// contained in the file "License.txt" accompanying this file.
//******************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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
    }
}
