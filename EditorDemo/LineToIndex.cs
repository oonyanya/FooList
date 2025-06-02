using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooProject.Collection;
using FooProject.Collection.DataStore;

namespace EditorDemo
{
    class LineToIndex : IRange
    {
        public long start { get; set; }
        public long length { get; set; }

        public LineToIndex(long index, long length)
        {
            start = index;
            this.length = length;
        }
        public LineToIndex()
        {
        }

        public IRange DeepCopy()
        {
            return new LineToIndex(start, length);
        }
    }
    class LineToIndexTableSerializer : ISerializeData<FixedList<LineToIndex>>
    {
        public FixedList<LineToIndex> DeSerialize(byte[] inputData)
        {
            var memStream = new MemoryStream(inputData);
            var reader = new BinaryReader(memStream, Encoding.Unicode);
            var arrayCount = reader.ReadInt32();
            var maxcapacity = reader.ReadInt32();
            var array = new FixedRangeList<LineToIndex>(arrayCount, maxcapacity);
            for(int i = 0; i < arrayCount; i++)
            {
                var index = reader.ReadInt64();
                var length = reader.ReadInt64();
                array.Add(new LineToIndex(index, length));
            }
            return array;
        }

        public byte[] Serialize(FixedList<LineToIndex> data)
        {
            var output = new byte[data.Count * 16 + 4 + 4]; //int32のサイズは4byte、charのサイズ2byte
            var memStream = new MemoryStream(output);
            var writer = new BinaryWriter(memStream, Encoding.Unicode);
            writer.Write(data.Count);
            writer.Write(data.MaxCapacity);
            foreach(var item in data)
            {
                writer.Write(item.start);
                writer.Write(item.length);
            }
            writer.Close();
            memStream.Dispose();
            return output;
        }
    }
}
