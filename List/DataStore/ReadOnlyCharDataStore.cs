using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


#if NET6_0_OR_GREATER
using System.IO.Pipelines;
#endif

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// 読み取り専用文字列のためのデーターストア
    /// </summary>
    /// <example>使い方はLasyLoadListTestを参照してください</example>
    public class ReadOnlyCharDataStore : ReadonlyContentStoreBase<IComposableList<char>>
    {
        /// <summary>
        /// CharReaderのインスタンス。設定しないと一切動かないので注意
        /// </summary>
        public CharReader Reader { get; set; }

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="r">読み取り対象のCharReader</param>
        /// <param name="cachesize">キャッシュサイズ</param>
        public ReadOnlyCharDataStore(CharReader r,int cachesize = 128) : base(cachesize)
        {
            this.Reader = r;
        }

        protected override IComposableList<char> OnRead(long index, int bytes)
        {
            if (this.Reader == null)
                throw new InvalidOperationException("Reader must be set");

            byte[] array = ArrayPool<byte>.Shared.Rent(bytes);
            try
            {
                this.Reader.Stream.Position = index;
                this.Reader.Stream.Read(array, 0, bytes);
                var str = this.Reader.Encoding.GetString(array, 0, bytes);
                var list = new ReadOnlyComposableList<char>(str);
                return list;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public bool IsCanCloneContent(IPinableContainer<IComposableList<char>> pin)
        {
            if (pin.ID == ReadonlyContentStoreBase<char>.DEFAULT_ID)
            {
                return true;
            }
            else
            {
                return base.IsCanCloneContent(pin);
            }
        }

        public override IPinableContainer<IComposableList<char>> Clone(IPinableContainer<IComposableList<char>> pin, IComposableList<char> cloned_content)
        {
            if (pin.ID == ReadonlyContentStoreBase<char>.DEFAULT_ID)
            {
                if(pin.Content == null)
                    return base.Clone(pin, cloned_content);
                var list = new ReadOnlyComposableList<char>(cloned_content);
                return base.Clone(pin, list);
            }
            else
            {
                return base.Clone(pin, cloned_content);
            }
        }

        /// <summary>
        /// 全ての処理が完了したことを表す。LoadAsyncを使用しない場合は呼び出す必要がない。
        /// </summary>
        /// <returns></returns>
        public async Task CompleteAsync()
        {
            if (this.Reader == null)
                throw new InvalidOperationException("Reader must be set");

            await this.Reader.CompleteAsync();
        }
    }
}
