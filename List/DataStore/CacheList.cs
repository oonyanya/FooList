using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    internal class CacheList<K,V>
    {
        Queue<K> queue = new Queue<K>();
        Dictionary<K,V> store = new Dictionary<K,V>();

        public int Limit { get; set; }

        public CacheList()
        {
            this.Limit = 128;
        }

        public bool TryGet(K key,out V value)
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

        public bool Set(K key,V value, out V outed_item)
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
