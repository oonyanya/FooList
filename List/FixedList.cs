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

        public bool IsReadOnly => throw new NotImplementedException();

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

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
    }
}
