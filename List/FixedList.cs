using Slusser.Collections.Generic;
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
    public class FixedList<T> : IEnumerable<T>,IList<T>
    {
        GapBuffer<T> items;


        public FixedList() : this(4)
        {
        }

        public FixedList(int limit_capacity) : this(4, limit_capacity) 
        { 
        }

        public FixedList(int init_capacity = 4,int limit_capacity = int.MaxValue - 1)
        {
            items = new GapBuffer<T>(init_capacity,limit_capacity);
        }

        public T this[int i] { get { return items[i]; } set { items[i] = value; } }

        public int Count { get { return items.Count; } }

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            items.Add(item);
        }

        public void AddRange(IEnumerable<T> collection, int collection_length = -1)
        {
            items.AddRange(collection,collection_length);
        }

        public void Insert(int index, T item)
        {
            InsertRange(index, new T[1] { item });
        }

        public void InsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
        {
            items.InsertRange(index, collection, collection_length);
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public void RemoveRange(int index, int count)
        {
            items.RemoveRange(index, count);
        }

        public IEnumerable<T> GetRange(int index,int count)
        {
            return items.GetRage(index, count);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public void Clear()
        {
            items.Clear();
            items.TrimExcess();
        }

        public void TrimExcess()
        {
            items.TrimExcess();
        }

        public int IndexOf(T item)
        {
            return items.IndexOf(item);
        }

        public bool Contains(T item)
        {
            return (IndexOf(item) >= 0);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return items.Remove(item);
        }
    }
}
