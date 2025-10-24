using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// 固定可能なコンテナ―を表すインターフェイス
    /// </summary>
    /// <typeparam name="T">格納対象の型</typeparam>
    public interface IPinableContainer<T>
    {
        /// <summary>
        /// 格納対象のコンテント。nullの場合、ディスク等どこか別の所に存在しているので、IPinableContainerStore.Get()を呼び出さなければならない。
        /// </summary>
        T Content { get; }
        /// <summary>
        /// コンテントを削除する
        /// </summary>
        void RemoveContent();
        /// <summary>
        /// 削除されたことを表す
        /// </summary>
        bool IsRemoved { get; set; }
        /// <summary>
        /// IDを指定する。IDの使い方はストアごとに違うのでストアのドキュメントを参照すること。
        /// </summary>
        string ID { get; set; }
    }

    public class PinableContainer<T> : IPinableContainer<T>
    {
        internal const long NOTCACHED = -1;

        internal DiskAllocationInfo Info { get; set; }

        internal long CacheIndex { get; set; }

        /// <inheritdoc/>
        public T Content { get; internal set; }

        /// <inheritdoc/>
        public bool IsRemoved { get; set; }

        /// <inheritdoc/>
        public string ID { get; set; }

        /// <summary>
        /// コンストラクター。IPinableContainerStoreインターフェイスを継承したクラス内以外では使用しないこと。
        /// </summary>
        /// <param name="content"></param>
        public PinableContainer(T content)
        {
            Content = content;
            Info = null;
            CacheIndex = NOTCACHED;
            IsRemoved = false;
            ID = null;
        }

        /// <inheritdoc/>
        public void RemoveContent()
        {
            this.IsRemoved = true;
        }

    }
}
