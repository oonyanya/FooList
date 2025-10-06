using System;
using System.Collections.Generic;
using System.Text;
using FooProject.Collection;
using FooProject.Collection.DataStore;

namespace SharedDemoProgram
{
    class StringBufferSerializer : ISerializeData<IComposableList<char>>
    {
        public IComposableList<char> DeSerialize(byte[] inputData)
        {
            var memStream = new MemoryStream(inputData);
            var reader = new BinaryReader(memStream, Encoding.Unicode);
            var arrayCount = reader.ReadInt32();
            var maxcapacity = reader.ReadInt32();
            var array = new FixedList<char>(arrayCount, maxcapacity);
            array.AddRange(reader.ReadChars(arrayCount));
            return array;
        }

        public byte[] Serialize(IComposableList<char> data)
        {
            FixedList<char> list = (FixedList<char>)data;
            var output = new byte[data.Count * 2 + 4 + 4]; //int32のサイズは4byte、charのサイズ2byte
            var memStream = new MemoryStream(output);
            var writer = new BinaryWriter(memStream, Encoding.Unicode);
            writer.Write(list.Count);
            writer.Write(list.MaxCapacity);
            writer.Write(list.ToArray());
            writer.Close();
            memStream.Dispose();
            return output;
        }
    }
}
