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
#if NET6_0_OR_GREATER
    public static class DecoderExtension
    {
        public static void Convert(this Decoder decoder, in ReadOnlySequence<byte> bytes, Span<char> writer, int char_count, bool flush, out int totalBytesWritten, out int totalCharsWritten, out bool completed)
        {
            totalBytesWritten = 0;
            totalCharsWritten = 0;
            completed = false;

            if (bytes.IsSingleSegment)
            {
                decoder.Convert(bytes.FirstSpan, writer.Slice(0,char_count), flush, out totalBytesWritten, out totalCharsWritten, out completed);
            }
            else
            {
                ReadOnlySequence<byte> remainingBytes = bytes;
                int charsWritten = 0;
                int bytesWritten = 0;

                foreach (var mem in remainingBytes)
                {
                    decoder.Convert(mem.Span, writer, flush, out bytesWritten, out charsWritten, out completed);
                    totalBytesWritten += bytesWritten;
                    totalCharsWritten += charsWritten;
                }
            }
        }
    }
#endif
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
        /// <param name="stream">読み取り対象のストリーム</param>
        /// <param name="enc">エンコーディング</param>
        public ReadOnlyCharDataStore(Stream stream, Encoding enc,int cachesize = 128) : base(cachesize)
        {
            this.reader = new CharReader(stream, enc);
        }

        public override async Task<OnLoadAsyncResult<IComposableList<char>>> OnLoadAsync(int count)
        {
            return await this.reader.LoadAsync(count);
        }

        public override IComposableList<char> OnLoad(int count, out long index, out int read_bytes)
        {
            var result = this.reader.Load(count);
            index = result.Index;
            read_bytes = result.ReadBytes;
            return result.Value;
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
