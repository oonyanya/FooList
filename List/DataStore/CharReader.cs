using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;

#if NET6_0_OR_GREATER
using System.IO.Pipelines;
#endif

namespace FooProject.Collection.DataStore
{
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

    public ref struct LineEnumratorState
    {
        public ReadOnlySpan<char> Current { get; private set; }
        public bool hasLineFeed { get; private set; }
        public LineEnumratorState(ReadOnlySpan<char> current, bool hasLineFeed)
        {
            this.Current = current;
            this.hasLineFeed = hasLineFeed;
        }
    }

    public ref struct LineEnumrator
    {
        bool isActive;
        ReadOnlySpan<char> reamin,newline;

        public LineEnumrator(ReadOnlySpan<char> chars,ReadOnlySpan<char> linefeed)
        {
            reamin = chars;
            newline = linefeed;
            Current = default;
            isActive = true;
        }
        public LineEnumrator GetEnumerator() => this;

        public LineEnumratorState Current { get; private set; }

        public bool MoveNext()
        {
            if(isActive == false)
            {
                return false;
            }
            int index = reamin.IndexOf(newline);
            if (index == -1)
            {
                Current = new LineEnumratorState(reamin, false);
                isActive = false;
            }
            else
            {
                Current = new LineEnumratorState(reamin.Slice(0, index),true);
                reamin = reamin.Slice(index + newline.Length);
            }
            return true;
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
    /// 文字列の読出しを行う
    /// </summary>
    public class CharReader
    {
        Stream stream;
        Encoding _encoding;
        Decoder _decoder;
        long _leastLoadPostion;
#if NET6_0_OR_GREATER
        PipeReader _pipeReader;
#endif

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="stream">読み取り対象のストリーム</param>
        /// <param name="enc">エンコーディング</param>
        public CharReader(Stream stream, Encoding enc)
        {
            this.stream = stream;
            _encoding = enc;
            _decoder = enc.GetDecoder();
            _leastLoadPostion = 0;
#if NET6_0_OR_GREATER
            this._pipeReader = PipeReader.Create(stream);
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

        /// <summary>
        /// 変換対象の改行コード
        /// </summary>
        public char[] LineFeed { get; set; }

        /// <summary>
        /// 変換元の改行コード
        /// </summary>
        public char[] NormalizedLineFeed {  get; set; }

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

        private int NormalizeLineFeed(Span<char> chars, IBufferWriter<char> writer, ReadOnlySpan<char> lineFeed,ReadOnlySpan<char> normalized_linefeed)
        {
            int total_written_chars = 0;
            var enumrator = new LineEnumrator(chars, lineFeed);
            foreach(var line in enumrator)
            {
                writer.Write(line.Current);
                total_written_chars += line.Current.Length;
                if (line.hasLineFeed)
                {
                    writer.Write(normalized_linefeed);
                    total_written_chars += normalized_linefeed.Length;
                }
            }
            return total_written_chars;
        }

        /// <summary>
        /// 文字を読み取る
        /// </summary>
        /// <param name="count">読み取る文字数</param>
        /// <returns>OnLoadAsyncResultを返す。何も読み取らなかった場合についてはOnLoadAsyncResultを参照すること</returns>
        public async Task<OnLoadAsyncResult<IEnumerable<char>>> LoadAsync(int count)
        {
#if NET6_0_OR_GREATER
            int byte_array_len = _encoding.GetMaxByteCount(count);
            ArrayBufferWriter<char> arrayBufferWriter = new ArrayBufferWriter<char>(count);
            char[] temp_buffer_writer = ArrayPool<char>.Shared.Rent(count);
            int leftCount = count;

            try
            {
                long index = _leastLoadPostion;
                int totalConvertedBytes = 0;
                int totalConvertedChars = 0;

                while (leftCount > 0)
                {
                    stream.Position = _leastLoadPostion;

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
                    int temp_converted_chars;
                    bool completed;
                    _decoder.Convert(skippedReadOnlyBuffer.Slice(0, Math.Min(byte_array_len, skippedReadOnlyBuffer.Length)), temp_buffer_writer.AsSpan(), Math.Min(count, leftCount), false, out converted_bytes, out temp_converted_chars, out completed);

                    if (LineFeed != null && NormalizeLineFeed != null)
                    {
                        converted_chars = NormalizeLineFeed(temp_buffer_writer.AsSpan().Slice(0, temp_converted_chars), arrayBufferWriter, LineFeed.AsSpan(), NormalizedLineFeed.AsSpan());
                    }
                    else
                    {
                        arrayBufferWriter.Write(temp_buffer_writer.AsSpan().Slice(0,temp_converted_chars));
                        converted_chars = temp_converted_chars;
                    }

                    _pipeReader.AdvanceTo(skippedReadOnlyBuffer.Slice(0, converted_bytes).End);
                    leftCount -= converted_chars;

                    totalConvertedBytes += converted_bytes;
                    totalConvertedChars += converted_chars;

                    _leastLoadPostion += converted_bytes;
                }

                var list = arrayBufferWriter.WrittenSpan.ToArray().Take(totalConvertedChars);

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
                ArrayPool<char>.Shared.Return(temp_buffer_writer);
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
            var arrayBufferWriter = new PooledArrayBufferWriter<char>(count);
            char[] temp_buffer_writer = ArrayPool<char>.Shared.Rent(count);
            int read_bytes = 0;

            try
            {
                stream.Position = _leastLoadPostion;

                long index = _leastLoadPostion;
                var stream_read_bytes = stream.Read(byte_array, 0, byte_array.Length);
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

                int temp_converted_chars;
                _decoder.Convert(byte_array, fetch_index, stream_read_bytes - fetch_index, temp_buffer_writer, 0, count, false, out converted_bytes, out temp_converted_chars, out completed);

                if (LineFeed != null && NormalizedLineFeed != null)
                {
                    converted_chars = NormalizeLineFeed(temp_buffer_writer.AsSpan(), arrayBufferWriter, LineFeed.AsSpan(), NormalizedLineFeed.AsSpan());
                }
                else
                {
                    arrayBufferWriter.Write(temp_buffer_writer.AsSpan());
                    converted_chars = temp_converted_chars;
                }

                _leastLoadPostion += converted_bytes;
                read_bytes = converted_bytes;

                return new OnLoadAsyncResult<IEnumerable<char>>(arrayBufferWriter.WrittenSpan.ToArray().Take(converted_chars), index, read_bytes);
            }
            finally
            {
                //返却しないとメモリーリークする
                ArrayPool<byte>.Shared.Return(byte_array);
                ArrayPool<char>.Shared.Return(temp_buffer_writer);
                arrayBufferWriter.Dispose();
            }
        }
    }
}
