using System;
using System.Collections.Generic;

namespace FooProject.Collection.DataStore
{
    internal class CacheOutedEventArgs<K, V> : EventArgs
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

    internal interface ICacheList<K, V>
    {
        Action<CacheOutedEventArgs<K, V>> CacheOuted { get; set; }
        int Limit { get; set; }

        void Flush();
        IEnumerable<V> ForEachValue();
        bool Set(K key, V value);
        bool Set(K key, V value, out V outed_item);
        bool TryGet(K key, out V value);
    }
}