using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection
{
    /// <summary>
    /// Fixed capacity List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedList<T> : IEnumerable<T>,ICollection<T>
    {
        T[] items;
        int size;

        public FixedList(int capacity = 4)
        { 
            items = new T[capacity];
        }

        public T this[int i] { get { return items[i]; } set { items[i] = value; } }

        public int Count { get { return size; } }

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (size >= items.Length)
                throw new InvalidOperationException("capacity over");
            items[size++] = item;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach(var item in collection)
                Add(item);
        }

        public void Insert(int index, T item)
        {
            InsertRange(index, new T[1] { item });
        }

        public void InsertRange(int index, ICollection<T> collection)
        {
            int collection_length = collection.Count;
            if (size + collection_length > items.Length)
            {
                throw new InvalidOperationException("capacity over");
            }
            else
            {
                Array.Copy(items, index, items, index + collection.Count, size - index);
            }

            int i = index;
            foreach (var item in collection)
            {
                items[i++] = item;
            }
            size += collection_length;
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public void RemoveRange(int index, int count)
        {
            if(index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            Array.Copy(items, index + count, items, index, size - (index + count));
            size -= count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < size; i++)
                yield return items[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < size; i++)
                yield return items[i];
        }

        public void Clear()
        {
            size = 0;
        }

        public int IndexOf(T item)
        {
            int index = 0;
            foreach (T x in this)
            {
                if (EqualityComparer<T>.Default.Equals(x, item))
                {
                    return index;
                }
                ++index;
            }

            // didn't find any item that matches.
            return -1;
        }

        public bool Contains(T item)
        {
            return (IndexOf(item) >= 0);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int count = this.Count;

            if (count == 0)
                return;

            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex must not be negative");
            if (arrayIndex >= array.Length || count > array.Length - arrayIndex)
                throw new ArgumentException("array too small");

            int index = arrayIndex, i = 0;
            foreach (T item in this)
            {
                if (i >= count)
                    break;

                array[index] = item;
                ++index;
                ++i;
            }
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
