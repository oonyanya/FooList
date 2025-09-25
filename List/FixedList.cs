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
    public class FixedList<T> : IComposableList<T>
    {
        protected private GapBuffer<T> collection;


        public FixedList() : this(4)
        {
        }

        public FixedList(int limit_capacity) : this(4, limit_capacity) 
        { 
        }

        public FixedList(int init_capacity = 4,int limit_capacity = int.MaxValue - 1)
        {
            collection = new GapBuffer<T>(init_capacity,limit_capacity);
        }

        public virtual T this[int i] { get { return collection[i]; } set { collection[i] = value; } }

        public int Count { get { return collection.Count; } }

        public int MaxCapacity { get { return collection.MaxCapacity; } set { collection.MaxCapacity = value; } }

        public bool IsReadOnly => false;

        public bool QueryAddRange(IEnumerable<T> collection, int collection_length = -1) { return this.Count + collection_length <= this.MaxCapacity; }

        public bool QueryInsertRange(int index, IEnumerable<T> collection, int collection_length = -1) { return this.Count + collection_length <= this.MaxCapacity; }

        public bool QueryUpdate(int index, T item) { return true; }

        public bool QueryRemoveRange(int index, int count) { return true; }

        public virtual void Add(T item)
        {
            collection.Add(item);
        }

        public virtual void AddRange(IEnumerable<T> collection, int collection_length = -1)
        {
            this.collection.AddRange(collection, collection_length);
        }

        public virtual void Insert(int index, T item)
        {
            this.collection.Insert(index, item);
        }

        public virtual void InsertRange(int index, IEnumerable<T> collection, int collection_length = -1)
        {
            this.collection.InsertRange(index, collection, collection_length);
        }

        public virtual void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public virtual void RemoveRange(int index, int count)
        {
            collection.RemoveRange(index, count);
        }

        public IEnumerable<T> GetRange(int index,int count)
        {
            return collection.GetRage(index, count);
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        public virtual void Clear()
        {
            collection.Clear();
            collection.TrimExcess();
        }

        public void TrimExcess()
        {
            collection.TrimExcess();
        }

        public int IndexOf(T item)
        {
            return collection.IndexOf(item);
        }

        public bool Contains(T item)
        {
            return (IndexOf(item) >= 0);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return collection.Remove(item);
        }
    }
}
