using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    public class FIFOCacheList<K, V> : ICacheList<K, V>
    {
        Queue<K> queue = new Queue<K>();
        Dictionary<K, V> store = new Dictionary<K, V>();

        public int Limit { get; set; }

        public FIFOCacheList()
        {
            this.Limit = 128;
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
            if (this.store.ContainsKey(key))
            {
                value = this.store[key];
                return true;
            }
            else
            {
                value = default(V);
                return false;
            }
        }

        public void Flush()
        {
            while (this.queue.Count > 0)
            {
                var outed_key = this.queue.Dequeue();
                if (this.store.ContainsKey(outed_key))
                {
                    var outed_item = this.store[outed_key];
                    this.store.Remove(outed_key);
                    this.OnCacheOuted(outed_key, outed_item, true);
                }
            }
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
                return false;
            }
            else
            {
                this.store.Add(key, value);
                this.queue.Enqueue(key);
                if (this.queue.Count > this.Limit)
                {
                    var outed_key = this.queue.Dequeue();
                    if (this.store.ContainsKey(outed_key))
                    {
                        outed_item = this.store[outed_key];
                        this.store.Remove(outed_key);
                        this.OnCacheOuted(outed_key, outed_item, true);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
