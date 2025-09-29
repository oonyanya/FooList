using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using FooProject.Collection;
using FooProject.Collection.DataStore;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// 読み取り専用文字列のためのデーターストア
    /// </summary>
    /// <example>使い方はLasyLoadListTestを参照してください</example>
    public class ReadOnlyCharDataStore : ReadonlyContentStoreBase<IComposableList<char>>
    {
        Stream stream;
        Encoding _encoding;
        Decoder _decoder;
        long _leastLoadPostion;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="stream">読み取り対象のストリーム</param>
        /// <param name="enc">エンコーディング</param>
        public ReadOnlyCharDataStore(Stream stream, Encoding enc) : base(8)
        {
            this.stream = stream;
            _encoding = enc;
            _decoder = enc.GetDecoder();
            _leastLoadPostion = 0;
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

        public override IComposableList<char> OnLoad(int count, out long index, out int read_bytes)
        {
            int byte_array_len = _encoding.GetMaxByteCount(count);
            byte[] byte_array = new byte[byte_array_len];

            stream.Position = _leastLoadPostion;

            index = _leastLoadPostion;
            stream.Read(byte_array, 0, byte_array.Length);

            int fetch_index = GetFetchIndexWithoutPreamble(byte_array, _encoding);
            if (fetch_index > 0)
            {
                index += fetch_index;
                _leastLoadPostion += fetch_index;
            }

            var char_array = new char[count];

            int acutal_bytes, actual_chars;
            bool completed;
            _decoder.Convert(byte_array, fetch_index, byte_array_len - fetch_index, char_array, 0, char_array.Length, false, out acutal_bytes, out actual_chars, out completed);

            _leastLoadPostion += acutal_bytes;
            read_bytes = acutal_bytes;

            return new ReadOnlyComposableList<char>(char_array);
        }

        public override IComposableList<char> OnRead(long index, int bytes)
        {
            byte[] array = new byte[bytes];
            stream.Position = index;
            stream.Read(array, 0, bytes);
            var str = _encoding.GetString(array, 0, bytes);
            var list = new ReadOnlyComposableList<char>(str);
            return list;
        }
    }
}
