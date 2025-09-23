using System;
using System.Collections.Generic;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// 全てのキャッシュ用クラスで共通の値を設定する
    /// </summary>
    public class CacheParameters
    {
        /// <summary>
        /// キャッシュの最低値
        /// </summary>
        public const int MINCACHESIZE = 4;
    }

    public class CacheOutedEventArgs<K, V> : EventArgs
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

    public interface ICacheList<K, V>
    {
        /// <summary>
        /// キャッシュアウト時に呼び出される
        /// </summary>
        event Action<CacheOutedEventArgs<K, V>> CacheOuted;

        /// <summary>
        /// 最大容量
        /// </summary>
        int Limit { get; set; }

        /// <summary>
        /// キャッシュの中身をすべて破棄する
        /// </summary>
        void Flush();
        /// <summary>
        /// キャッシュの内容を全て列挙する
        /// </summary>
        /// <returns>Vの列挙子</returns>
        IEnumerable<V> ForEachValue();
        /// <summary>
        /// キャッシュにセットする
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <returns>キャッシュからあふれた値があれば真、そうでなければ偽を返す</returns>
        bool Set(K key, V value);
        /// <summary>
        /// キャッシュにセットする
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="value">値</param>
        /// <param name="outed_item">キャッシュからあふれた値がセットされる</param>
        /// <returns>キャッシュからあふれた値があれば真、そうでなければ偽を返す</returns>
        bool Set(K key, V value, out V outed_item);
        /// <summary>
        /// キャッシュから取得する
        /// </summary>
        /// <param name="key">取得対象のキー</param>
        /// <param name="value">取得された値をセットする。なにもなければ、default(T)がセットされる</param>
        /// <returns>キャッシュに存在すれば真、そうでなければ偽を返す</returns>
        bool TryGet(K key, out V value);
    }
}