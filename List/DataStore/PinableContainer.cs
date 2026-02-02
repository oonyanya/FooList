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
        /// コンテントを書き込む
        /// </summary>
        void WriteContent();
        /// <summary>
        /// 削除されたことを表す
        /// </summary>
        bool IsRemoved { get; set; }
        /// <summary>
        /// 書き込み要求がされたことを表す
        /// </summary>
        bool IsRequireWrited { get; set; }
        /// <summary>
        /// IDを指定する。IDの使い方はストアごとに違うのでストアのドキュメントを参照すること。
        /// </summary>
        string ID { get; set; }
    }

    [Flags]
    public enum PinableContainerFlags
    {
        None = 0,
        Removed = 1,
        Writed = 2,
    }

    public class PinableContainer<T> : IPinableContainer<T>
    {
        internal const long NOTCACHED = -1;

        internal DiskAllocationInfo Info { get; set; }

        internal long CacheIndex { get; set; }

        internal PinableContainerFlags Flags { get; private set; }

        /// <inheritdoc/>
        public T Content { get; internal set; }

        /// <inheritdoc/>
        public bool IsRemoved {
            get { return this.Flags.HasFlag(PinableContainerFlags.Removed); }
            set
            {
                if (value)
                {
                    this.Flags |= PinableContainerFlags.Removed;
                }
                else
                {
                    this.Flags = PinableContainerFlags.None;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsRequireWrited
        {
            get { return this.Flags.HasFlag(PinableContainerFlags.Writed); }
            set {
                if (value)
                    this.Flags |= PinableContainerFlags.Writed;
                else
                    this.Flags = PinableContainerFlags.None;
            }
        }

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
            Flags = PinableContainerFlags.None;
            ID = null;
        }

        /// <inheritdoc/>
        public void RemoveContent()
        {
            this.IsRemoved = true;
        }

        public void WriteContent()
        {
            this.IsRequireWrited = true;
        }
    }
}
