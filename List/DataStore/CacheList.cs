using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    internal class CacheList<K,V>
    {
        Queue<V> queue = new Queue<V>();
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
            }
            else
            {
                this.store.Add(key, value);
            }
            this.queue.Enqueue(value);
            if (this.queue.Count > this.Limit) 
            { 
                outed_item = this.queue.Dequeue();
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
