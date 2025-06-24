/*
 * 2q a low overhead high performance buffer management replacement algorithm
 * 
 * 論文：
 * https://arpitbhayani.me/blogs/2q-cache/
 * https://www.vldb.org/conf/1994/P439.PDF
 * 
 * 実装は
 * https://github.com/Zetonem/2Q-Cache-Algorithm
 * からコピペ
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    internal class LRUCache<K> : IEnumerable<K>
    {
        Dictionary<K,LinkedListNode<K>> store = new Dictionary<K, LinkedListNode<K>>();
        LinkedList<K> list = new LinkedList<K>();

        public int Limit { get; set; }

        public LRUCache()
        {
            Limit = 128;
        }

        public bool Contains(K key)
        {
            return store.ContainsKey(key);
        }

        public bool Set(K key,out K outed_key)
        {
            outed_key = default(K);
            LinkedListNode<K> node;
            bool has_key = store.TryGetValue(key, out node);
            if(has_key)
            {
                list.Remove(node);
                list.AddFirst(node);
            }
            else
            {
                node = list.AddFirst(key);
                store.Add(key, node);
            }

            if (list.Count >= Limit)
            {
                outed_key = list.Last.Value;
                this.store.Remove(outed_key);
                list.RemoveLast();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            this.store.Clear();
            this.list.Clear();
        }

        public IEnumerator<K> GetEnumerator()
        {
            foreach(var item in list)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class FIFOCache<K> : IEnumerable<K>
    {
        Dictionary<K, LinkedListNode<K>> store = new Dictionary<K, LinkedListNode<K>>();
        LinkedList<K> list = new LinkedList<K>();

        public K Last { get { return list.Last.Value; } }

        public int Count { get { return list.Count; } }

        public bool Contains(K key)
        {
            return store.ContainsKey(key);
        }

        public void Add(K key)
        {
            if (store.ContainsKey(key))
                return;

            var newNode = list.AddFirst(key);
            store.Add(key, newNode);
        }

        public void Remove(K key)
        {
            LinkedListNode<K> node;
            bool has_key = store.TryGetValue(key, out node);
            if (has_key)
            {
                this.list.Remove(node);
                this.store.Remove(key);
            }
            return;
        }

        public void Clear()
        {
            this.store.Clear();
            this.list.Clear();
        }

        public IEnumerator<K> GetEnumerator()
        {
            foreach(var item in list)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class TwoQueueCacheList<K, V> : ICacheList<K, V>
    {
        const int defaultLimit = 128;

        LRUCache<K> lru = new LRUCache<K>();
        LinkedList<K> inQueue = new LinkedList<K>();
        FIFOCache<K> outQueque = new FIFOCache<K>();
        Dictionary<K, V> store = new Dictionary<K, V>();

        int _limit;
        int _inQLimit, _outQLimit;
        public int Limit
        {
            get
            {
                return _limit;
            }
            set
            {
                if (value < 4)
                    throw new ArgumentOutOfRangeException("Limit must be more than 4");
                //論文によるこのくらいが一番いいらしい
                var inQLimit = Math.Max((int)(value * 0.2), 1);
                var outQLimit = Math.Max((int)(value * 0.6), 1);
                var lru_limit = Math.Max(value - inQLimit - outQLimit, 1);

                this._inQLimit = inQLimit;
                this._outQLimit = outQLimit;
                this.lru.Limit = lru_limit;
                this._limit = value;
            }
        }

        public TwoQueueCacheList()
        {
            this.Limit = defaultLimit;
        }

        public event Action<CacheOutedEventArgs<K, V>> CacheOuted;

        void OnCacheOuted(K key, V value, bool requireWriteBack)
        {
            if (CacheOuted != null)
                CacheOuted(new CacheOutedEventArgs<K, V>(key, value, requireWriteBack));
        }

        public IEnumerable<V> ForEachValue()
        {
            foreach (V value in store.Values)
            {
                yield return value;
            }
        }

        public bool TryGet(K key, out V value)
        {
            value = default(V);
            if (this.store.ContainsKey(key))
            {
                value = this.store[key];
                return true;
            }
            return false;
        }

        public void Flush()
        {
            foreach (var outed_key in this.inQueue)
            {
                if (this.store.ContainsKey(outed_key))
                {
                    var outed_item = this.store[outed_key];
                    this.store.Remove(outed_key);
                    this.OnCacheOuted(outed_key, outed_item, true);
                }
            }
            this.inQueue.Clear();

            foreach (var outed_key in this.outQueque)
            {
                if (this.store.ContainsKey(outed_key))
                {
                    var outed_item = this.store[outed_key];
                    this.store.Remove(outed_key);
                    this.OnCacheOuted(outed_key, outed_item, true);
                }
            }
            this.outQueque.Clear();

            foreach (var outed_key in this.lru)
            {
                if (this.store.ContainsKey(outed_key))
                {
                    var outed_item = this.store[outed_key];
                    this.store.Remove(outed_key);
                    this.OnCacheOuted(outed_key, outed_item, true);
                }
            }
            this.lru.Clear();
        }

        public bool Set(K key, V value)
        {
            V _;
            return this.Set(key, value, out _);
        }

        public bool Set(K key, V value, out V outed_item)
        {
            outed_item = default(V);
            if (this.store.ContainsKey(key))
            {
                this.store[key] = value;

                K outed_key;
                if (this.lru.Contains(key))
                {
                    var overflow = this.lru.Set(key, out outed_key);
                    System.Diagnostics.Debug.Assert(overflow == false);
                }
                else if (this.outQueque.Contains(key))
                {
                    var overflow = this.lru.Set(key, out outed_key);
                    this.outQueque.Remove(key);

                    //LRUから溢れたら
                    if (overflow)
                    {
                        var removedValue = this.store[outed_key];
                        this.OnCacheOuted(outed_key, removedValue, true);
                        this.store.Remove(outed_key);
                    }
                }
                return false;
            }

            bool hasFreeslot = false;

            if (this.inQueue.Count < this._inQLimit)
            {
                this.inQueue.AddFirst(key);
                hasFreeslot = true;
            }
            else if (this.outQueque.Count < this.Limit)
            {
                this.outQueque.Add(key);
                hasFreeslot = true;
            }

            if (hasFreeslot)
            {
                this.store.Add(key, value);
                return false;
            }

            K lastKeyInQ = this.inQueue.Last.Value;
            this.inQueue.RemoveLast();

            this.outQueque.Add(lastKeyInQ);

            bool hasOverflow = false;
            if (this.outQueque.Count > this._outQLimit)
            {
                K lastKeyQutQ = this.outQueque.Last;
                if (this.store.ContainsKey(lastKeyQutQ))
                {
                    outed_item = this.store[lastKeyQutQ];
                    this.OnCacheOuted(lastKeyQutQ, outed_item, true);
                    this.store.Remove(lastKeyQutQ);
                    this.outQueque.Remove(lastKeyQutQ);
                    hasOverflow = true;
                }
                else
                {
                    throw new InvalidOperationException($"No value in cache for key {lastKeyQutQ}");
                }
            }

            this.inQueue.AddFirst(key);
            this.store.Add(key, value);

            return hasOverflow;
        }
    }

}
