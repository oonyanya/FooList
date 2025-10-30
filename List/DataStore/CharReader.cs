using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using System.Reflection;


#if NET6_0_OR_GREATER
using System.IO.Pipelines;
#endif

namespace FooProject.Collection.DataStore
{
    //コピー元：https://gist.github.com/ladeak/71b0b4c59bdd4eb548535dc641729682#file-pooledarraybufferwriter-cs
    public sealed class PooledArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
    {
        private const int DefaultInitialBufferSize = 4096 * 2;

        private T[] _buffer;
        private int _index;
        private bool _disposed;

        public PooledArrayBufferWriter()
        {
            _buffer = ArrayPool<T>.Shared.Rent(DefaultInitialBufferSize);
            _index = 0;
            _disposed = false;
        }

        public PooledArrayBufferWriter(int count)
        {
            _buffer = ArrayPool<T>.Shared.Rent(count);
            _index = 0;
            _disposed = false;
        }

        public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

        public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

        public int WrittenCount => _index;

        public int Capacity => _buffer.Length;

        public int FreeCapacity => _buffer.Length - _index;

        public void Clear()
        {
            if (_disposed) throw new InvalidOperationException("");
            ArrayPool<T>.Shared.Return(_buffer);
            _buffer = ArrayPool<T>.Shared.Rent(DefaultInitialBufferSize);
            _index = 0;
        }

        public void Advance(int count)
        {
            if (_disposed) throw new InvalidOperationException("");
            if (count < 0)
                throw new ArgumentException(null, nameof(count));

            if (_index > _buffer.Length - count)
                ThrowInvalidOperationException_AdvancedTooFar();

            _index += count;
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            if (_disposed) throw new InvalidOperationException("");
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsMemory(_index);
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            if (_disposed) throw new InvalidOperationException("");
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (sizeHint > FreeCapacity)
            {
                int currentLength = _buffer.Length;

                int growBy = Math.Max(sizeHint, currentLength);

                int newSize = currentLength + growBy;

                var temp = ArrayPool<T>.Shared.Rent(newSize);
                Array.Copy(_buffer, temp, _index);
                ArrayPool<T>.Shared.Return(_buffer);
                _buffer = temp;
            }
        }

        private static void ThrowInvalidOperationException_AdvancedTooFar() => throw new InvalidOperationException();

        public void Dispose()
        {
            if(_disposed == false)
            {
                ArrayPool<T>.Shared.Return(_buffer);
                _buffer = null;
                _disposed = true;
            }
        }
    }

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
                decoder.Convert(bytes.FirstSpan, writer.Slice(0, char_count), flush, out totalBytesWritten, out totalCharsWritten, out completed);
            }
            else
            {
                ReadOnlySequence<byte> remainingBytes = bytes;
                Span<char> reaminWriter = writer;
                int charsWritten = 0;
                int bytesWritten = 0;
                int index = 0;

                foreach (var mem in remainingBytes)
                {
                    decoder.Convert(mem.Span, reaminWriter, flush, out bytesWritten, out charsWritten, out completed);
                    totalBytesWritten += bytesWritten;
                    totalCharsWritten += charsWritten;
                    if (totalCharsWritten >= char_count)
                        break;
                    index += charsWritten;
                    reaminWriter = writer.Slice(index, char_count - index);
                }
            }
        }
    }
#endif
    /// <summary>
    /// 文字列の読出しを行う
    /// </summary>
    public class CharReader
    {
        const int REFECTH_BUFFER_SIZE_RAITO = 2;

        Stream stream;
        Encoding _encoding;
        Decoder _decoder;
        long _leastLoadPostion;
#if NET6_0_OR_GREATER
        int buffer_size;
        byte[] _lineFeedBinary;
        PipeReader _pipeReader;
#endif

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="stream">読み取り対象のストリーム</param>
        /// <param name="enc">エンコーディング</param>
        public CharReader(Stream stream, Encoding enc, int buffer_size = -1)
        {
            this.stream = stream;
            _encoding = enc;
            _decoder = enc.GetDecoder();
            _leastLoadPostion = 0;
#if NET6_0_OR_GREATER
            if (buffer_size > 0) {
                this.buffer_size = buffer_size;
            }
            else
            {
                this.buffer_size = 4096;
            }
            var pipeReaderOptions = new StreamPipeReaderOptions(bufferSize: this.buffer_size);
            this._pipeReader = PipeReader.Create(stream, pipeReaderOptions);
#endif
        }

        /// <summary>
        /// 読み取り対象のストリーム
        /// </summary>
        public Stream Stream { get { return stream; } }

        /// <summary>
        /// エンコーディング
        /// </summary>
        public Encoding Encoding { get { return _encoding; } }

#if NET6_0_OR_GREATER
        private ReadOnlySequence<byte> SkipPreaemble(ReadOnlySequence<byte> buffer, ReadOnlySpan<byte> preaemble, out bool skipped)
        {
            var skipLength = 0;
            int preaembleIndex = 0;
            skipped = false;
            foreach (var mem in buffer)
            {
                if (preaembleIndex >= preaemble.Length)
                    break;
                for (int i = 0; i < mem.Span.Length && preaembleIndex < preaemble.Length; i++)
                {
                    if (mem.Span[i] == preaemble[preaembleIndex])
                    {
                        skipLength++;
                    }
                    preaembleIndex++;
                }
            }
            if (skipLength > 0)
            {
                skipped = true;
            }
            return buffer.Slice(skipLength);
        }
#endif

        /// <summary>
        /// 文字を読み取る
        /// </summary>
        /// <param name="count">読み取る文字数</param>
        /// <returns>OnLoadAsyncResultを返す。何も読み取らなかった場合についてはOnLoadAsyncResultを参照すること</returns>
        public async Task<OnLoadAsyncResult<IEnumerable<char>>> LoadAsync(int count)
        {
#if NET6_0_OR_GREATER
            int byte_array_len = _encoding.GetMaxByteCount(count);
            PooledArrayBufferWriter<char> arrayBufferWriter = new PooledArrayBufferWriter<char>(count);
            int leftCount = count;

            try
            {
                long index = _leastLoadPostion;
                int totalConvertedBytes = 0;
                int totalConvertedChars = 0;

                stream.Position = _leastLoadPostion;

                while (leftCount > 0)
                {
                    var bufferResult = await _pipeReader.ReadAsync().ConfigureAwait(false);

                    if (bufferResult.IsCompleted && bufferResult.Buffer.Length == 0)
                    {
                        _decoder.Reset();
                        break;
                    }

                    var skippedReadOnlyBuffer = bufferResult.Buffer;

                    if (_leastLoadPostion == 0 && _encoding.Preamble.Length > 0)
                    {
                        bool skipped;
                        skippedReadOnlyBuffer = SkipPreaemble(bufferResult.Buffer, _encoding.Preamble, out skipped);
                        if (skipped)
                        {
                            var preambleBufferPosition = bufferResult.Buffer.GetPosition(_encoding.Preamble.Length);
                            _pipeReader.AdvanceTo(preambleBufferPosition);
                            _leastLoadPostion += _encoding.Preamble.Length;
                            index += _encoding.Preamble.Length;
                        }
                    }

                    int converted_bytes, converted_chars;
                    bool completed;
                    _decoder.Convert(skippedReadOnlyBuffer.Slice(0, Math.Min(byte_array_len, skippedReadOnlyBuffer.Length)), arrayBufferWriter.GetSpan(), Math.Min(leftCount, count), false, out converted_bytes, out converted_chars, out completed);
                    arrayBufferWriter.Advance(converted_chars);

                    totalConvertedChars += converted_chars;
                    leftCount -= converted_chars;

                    _pipeReader.AdvanceTo(skippedReadOnlyBuffer.Slice(0, converted_bytes).End);

                    totalConvertedBytes += converted_bytes;

                    _leastLoadPostion += converted_bytes;

                    stream.Position = _leastLoadPostion;
                }

                IEnumerable<char> list;
                list = arrayBufferWriter.WrittenSpan.ToArray().Take(totalConvertedChars);

                if (totalConvertedChars == 0)
                {
                    return new OnLoadAsyncResult<IEnumerable<char>>(null, 0, 0);
                }
                else
                {
                    return new OnLoadAsyncResult<IEnumerable<char>>(list, index, totalConvertedBytes);
                }
            }
            finally
            {
                arrayBufferWriter.Dispose();
            }
#else
            throw new NotSupportedException(".net 6.0以降を使用してください");
#endif
        }

        /// <summary>
        /// 読み取りが終了したことを通知する
        /// </summary>
        /// <returns></returns>
        public async Task CompleteAsync()
        {
#if NET6_0_OR_GREATER
            await this._pipeReader.CompleteAsync();
#else
            await Task.Delay(0);
#endif
        }

        private int GetFetchIndexWithoutPreamble(byte[] bytes, Encoding encoding)
        {
            var preamble = encoding.GetPreamble();
            if (bytes.Length < preamble.Length)
                throw new ArgumentOutOfRangeException($@"must be more than preamble length. encoding is {encoding.WebName}. preamble length is {preamble.Length}.");

            int index = 0;

            if (preamble.Length == 0)
                return index;

            for (int i = 0; i < preamble.Length; i++)
            {
                if (bytes[index] == preamble[index])
                {
                    index++;
                }
            }

            return index;
        }

        /// <summary>
        /// 文字を読み取る
        /// </summary>
        /// <param name="count">読み取る文字数</param>
        /// <returns>OnLoadAsyncResultを返す。何も読み取らなかった場合についてはOnLoadAsyncResultを参照すること</returns>
        public OnLoadAsyncResult<IEnumerable<char>> Load(int count)
        {
            int byte_array_len = _encoding.GetMaxByteCount(count);
            byte[] byte_array = ArrayPool<byte>.Shared.Rent(byte_array_len);
            char[] temp_buffer_writer = ArrayPool<char>.Shared.Rent(count);
            int read_bytes = 0;

            try
            {
                stream.Position = _leastLoadPostion;

                long index = _leastLoadPostion;
                int stream_read_bytes;
#if NET6_0_OR_GREATER
                stream_read_bytes = stream.Read(byte_array.AsSpan());
#else
                stream_read_bytes = stream.Read(byte_array, 0, byte_array.Length);
#endif
                if (stream_read_bytes == 0)
                {
                    _decoder.Reset();
                    //.NET standard 2.0以降だとfinallyに飛ぶので何もしなくていい
                    return new OnLoadAsyncResult<IEnumerable<char>>(null, 0, 0);
                }

                int fetch_index = GetFetchIndexWithoutPreamble(byte_array, _encoding);
                if (fetch_index > 0)
                {
                    index += fetch_index;
                    _leastLoadPostion += fetch_index;
                }

                int converted_bytes, converted_chars;
                bool completed;

#if NET6_0_OR_GREATER
                _decoder.Convert(byte_array.AsSpan().Slice(fetch_index, stream_read_bytes - fetch_index), temp_buffer_writer.AsSpan().Slice(0, count), false, out converted_bytes, out converted_chars, out completed);
#else
                _decoder.Convert(byte_array, fetch_index, stream_read_bytes - fetch_index, temp_buffer_writer, 0, count, false, out converted_bytes, out converted_chars, out completed);
#endif

                _leastLoadPostion += converted_bytes;
                read_bytes = converted_bytes;

                return new OnLoadAsyncResult<IEnumerable<char>>(temp_buffer_writer.ToArray().Take(converted_chars), index, read_bytes);
            }
            finally
            {
                //返却しないとメモリーリークする
                ArrayPool<byte>.Shared.Return(byte_array);
                ArrayPool<char>.Shared.Return(temp_buffer_writer);
            }
        }
    }
}
