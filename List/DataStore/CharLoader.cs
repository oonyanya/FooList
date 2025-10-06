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
    public class CharReader
    {
        Stream stream;
        Encoding _encoding;
        Decoder _decoder;
        long _leastLoadPostion;
#if NET6_0_OR_GREATER
        PipeReader _pipeReader;
#endif

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

        public Stream Stream { get { return stream; } }

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

        public async Task<OnLoadAsyncResult<IComposableList<char>>> LoadAsync(int count)
        {
#if NET6_0_OR_GREATER
            int byte_array_len = _encoding.GetMaxByteCount(count);
            ArrayBufferWriter<char> arrayBufferWriter = new ArrayBufferWriter<char>(count);
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
                    int skippedLength = 0;

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
                    _decoder.Convert(skippedReadOnlyBuffer.Slice(0, Math.Min(byte_array_len, skippedReadOnlyBuffer.Length)), arrayBufferWriter.GetSpan(), Math.Min(count, leftCount), false, out converted_bytes, out converted_chars, out completed);
                    arrayBufferWriter.Advance(converted_chars);
                    _pipeReader.AdvanceTo(skippedReadOnlyBuffer.Slice(0, converted_bytes).End);
                    leftCount -= converted_chars;

                    totalConvertedBytes += converted_bytes;
                    totalConvertedChars += converted_chars;

                    _leastLoadPostion += converted_bytes;
                }

                var list = new ReadOnlyComposableList<char>(arrayBufferWriter.WrittenSpan.ToArray());

                if (list.Count == 0)
                {
                    return new OnLoadAsyncResult<IComposableList<char>>(null, 0, 0);
                }
                else
                {
                    return new OnLoadAsyncResult<IComposableList<char>>(list, index, totalConvertedBytes);
                }
            }
            finally
            {
            }
#else
            throw new NotSupportedException(".net 6.0以降を使用してください");
#endif
        }

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

        public OnLoadAsyncResult<IComposableList<char>> Load(int count)
        {
            int byte_array_len = _encoding.GetMaxByteCount(count);
            byte[] byte_array = ArrayPool<byte>.Shared.Rent(byte_array_len);
            var char_array = ArrayPool<char>.Shared.Rent(count);
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
                    return new OnLoadAsyncResult<IComposableList<char>>(null, 0, 0);
                }

                int fetch_index = GetFetchIndexWithoutPreamble(byte_array, _encoding);
                if (fetch_index > 0)
                {
                    index += fetch_index;
                    _leastLoadPostion += fetch_index;
                }

                int converted_bytes, converted_chars;
                bool completed;
                _decoder.Convert(byte_array, fetch_index, stream_read_bytes - fetch_index, char_array, 0, count, false, out converted_bytes, out converted_chars, out completed);

                _leastLoadPostion += converted_bytes;
                read_bytes = converted_bytes;

                var list = new ReadOnlyComposableList<char>(char_array.Take(converted_chars));
                return new OnLoadAsyncResult<IComposableList<char>>(list, index, read_bytes);
            }
            finally
            {
                //返却しないとメモリーリークする
                ArrayPool<byte>.Shared.Return(byte_array);
                ArrayPool<char>.Shared.Return(char_array);
            }
        }
    }
}
