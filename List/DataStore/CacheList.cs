// https://github.com/Zetonem/2Q-Cache-Algorithm
// からコピペ
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
        public K LastRemoved {get; private set;}

        public int Limit { get; set; }

        public LRUCache()
        {
            Limit = 128;
        }

        public bool Contains(K key)
        {
            return store.ContainsKey(key);
        }

        public void Set(K key)
        {
            if(store.ContainsKey(key))
            {
                var node = store[key];
                list.Remove(node);
                list.AddFirst(node);
                return;
            }

            if(list.Count >= Limit)
            {
                var lastRemoveKey = list.Last.Value;
                this.LastRemoved = lastRemoveKey;
                this.store.Remove(lastRemoveKey);
                list.RemoveLast();
            }
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
            if (store.ContainsKey(key))
            {
                var node = store[key];
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

    internal class CacheOutedEventArgs<K,V> : EventArgs
    {
        public K Key { get; private set; }
        public V Value { get; private set; }
        public bool RequireWriteBack { get; private set; }
        public CacheOutedEventArgs(K key, V value, bool requireWriteBack)
        {
            Key = key;
            Value = value;
            RequireWriteBack = requireWriteBack;
        }
    }

    internal class CacheList<K,V>
    {
        //inQuequeとoutQuequeの比率は0.5なので、LRUキャッシュも含めると128エントリー確保していることになる。
        //詳しいことはLimitプロパティを参照すること。
        const int defaultLimit = 32;

        LRUCache<K> lru = new LRUCache<K>();
        LinkedList<K> inQueue = new LinkedList<K>();
        FIFOCache<K> outQueque = new FIFOCache<K>();
        Dictionary<K,V> store = new Dictionary<K,V>();

        int _limit;
        public int Limit
        {
            get
            {
                return _limit;
            }
            set
            {
                _limit = value;
                this.lru.Limit = value * 2;
            }
        }

        public CacheList()
        {
            this.Limit = defaultLimit;
        }

        public Action<CacheOutedEventArgs<K,V>> CacheOuted { get; set; }

        public void OnCacheOuted(K key, V value, bool requireWriteBack)
        {
            if (CacheOuted != null)
                CacheOuted(new CacheOutedEventArgs<K, V>(key,value,requireWriteBack));
        }

        public IEnumerable<V> ForEachValue()
        {
            foreach(V value in store.Values)
            {
                yield return value;
            }
        }

        public bool TryGet(K key,out V value)
        {
            value = default(V);
            if (this.store.ContainsKey(key))
            {
                value = this.store[key];
                if (this.lru.Contains(key))
                {
                    this.lru.Set(key);
                }
                else if (this.outQueque.Contains(key))
                {
                    {
                        this.lru.Set(key);
                        this.outQueque.Remove(key);

                        //LRUから溢れたら
                        var lastRemoved = this.lru.LastRemoved;
                        if (lastRemoved != null)
                        {
                            var removedValue = this.store[lastRemoved];
                            this.OnCacheOuted(lastRemoved, removedValue, false);
                            this.store.Remove(lastRemoved);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public void Flush()
        {
            foreach(var outed_key in this.inQueue)
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

        public bool Set(K key,V value, out V outed_item)
        {
            outed_item = default(V);
            if (this.store.ContainsKey(key))
            {
                this.store[key] = value;
                return false;
            }

            bool hasFreeslot = false;

            if(this.inQueue.Count < this.Limit)
            {
                this.inQueue.AddFirst(key);
                hasFreeslot = true;
            }

            if (this.outQueque.Count < this.Limit)
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
            if(this.outQueque.Count > this.Limit)
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
