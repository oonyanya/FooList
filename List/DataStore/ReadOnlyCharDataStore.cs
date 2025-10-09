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
        CharReader reader;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="r">読み取り対象のCharReader</param>
        /// <param name="cachesize">キャッシュサイズ</param>
        public ReadOnlyCharDataStore(CharReader r,int cachesize = 128) : base(cachesize)
        {
            this.reader = r;
        }

        public override async Task<OnLoadAsyncResult<IComposableList<char>>> OnLoadAsync(int count)
        {
            var r = await this.reader.LoadAsync(count);
            var result = new OnLoadAsyncResult<IComposableList<char>>(
                new ReadOnlyComposableList<char>(r.Value),r.Index,r.ReadBytes
                );
            return result;
        }

        public override IComposableList<char> OnLoad(int count, out long index, out int read_bytes)
        {
            var result = this.reader.Load(count);
            index = result.Index;
            read_bytes = result.ReadBytes;
            return new ReadOnlyComposableList<char>(result.Value);
        }

        public override IComposableList<char> OnRead(long index, int bytes)
        {
            byte[] array = ArrayPool<byte>.Shared.Rent(bytes);
            try
            {
                this.reader.Stream.Position = index;
                this.reader.Stream.Read(array, 0, bytes);
                var str = this.reader.Encoding.GetString(array, 0, bytes);
                var list = new ReadOnlyComposableList<char>(str);
                return list;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        /// <summary>
        /// 全ての処理が完了したことを表す。LoadAsyncを使用しない場合は呼び出す必要がない。
        /// </summary>
        /// <returns></returns>
        public async Task CompleteAsync()
        {
            await this.reader.CompleteAsync();
        }
    }
}
