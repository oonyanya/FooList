using Slusser.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    internal class DiskAllocationInfo
    {
        public long Index { get; set; }
        public int AlignedLength { get; set; }

        public DiskAllocationInfo(long index, int length)
        {
            Index = index;
            AlignedLength = length;
        }
    }

    public class DiskPinableContentDataStore<T> : IPinableContainerStore<T>
    {
        const int PAGESIZE = 32768;
        const int EMPTYLISTSIZE = 32;   //ひとまず2^32までとする

        string tempFilePath;
        long emptyIndex;
        ISerializeData<T> serializer;
        Stack<DiskAllocationInfo>[] emptylist = new Stack<DiskAllocationInfo>[EMPTYLISTSIZE];


        public DiskPinableContentDataStore(ISerializeData<T> serializer)
        {
            tempFilePath = System.IO.Path.GetTempFileName();
            this.serializer = serializer;
        }

        public PinnedContent<T> Get(PinableContainer<T> pinableContainer)
        {
            PinnedContent<T> result;
            if (this.TryGet(pinableContainer, out result))
                return result;
            else
                throw new ArgumentException();
        }

        public bool TryGet(PinableContainer<T> pinableContainer, out PinnedContent<T> result)
        {
            if (pinableContainer.Info.Index == -1 || pinableContainer.Content?.Equals(default(T)) == false)
            {
                result = new PinnedContent<T>(pinableContainer, this);
                return true;
            }

            var dataStream = File.Open(tempFilePath, FileMode.Open);

            var reader = new BinaryReader(dataStream);

            reader.BaseStream.Position = pinableContainer.Info.Index;

            int count = reader.ReadInt32();

            var data = reader.ReadBytes(count);

            pinableContainer.SetConent(this.serializer.DeSerialize(data));

            result = new PinnedContent<T>(pinableContainer, this);

            reader.Close();

            dataStream.Close();

            return true;
        }

        public static int Log2(int v)
        {
            int r = 0xFFFF - v >> 31 & 0x10;
            v >>= r;
            int shift = 0xFF - v >> 31 & 0x8;
            v >>= shift;
            r |= shift;
            shift = 0xF - v >> 31 & 0x4;
            v >>= shift;
            r |= shift;
            shift = 0x3 - v >> 31 & 0x2;
            v >>= shift;
            r |= shift;
            r |= (v >> 1);
            return r;
        }

        void SetEmptyList(DiskAllocationInfo Info)
        {
            int msb = Log2(Info.AlignedLength);

            if(this.emptylist[msb] == null)
            {
                this.emptylist[msb] = new Stack<DiskAllocationInfo>();
            }
            this.emptylist[msb].Push(Info);
        }

        DiskAllocationInfo GetEmptyList(int dataLength)
        {
            if(dataLength == -1)
                return null;

            int msb = Log2(dataLength);

            if (this.emptylist[msb] == null || this.emptylist[msb].Count == 0)
                return null;

            return this.emptylist[msb].Pop();
        }

        public void Set(PinableContainer<T> pinableContainer)
        {
            if (pinableContainer.Content == null)
            {
                this.SetEmptyList(pinableContainer.Info);
                pinableContainer.ReleaseInfo();
                return;
            }

            var dataStream = File.Open(tempFilePath, FileMode.Open);

            var writer = new BinaryWriter(dataStream);

            var data = this.serializer.Serialize(pinableContainer.Content);

            int dataLength = data.Length + 4;
            int alignedDataLength = dataLength + PAGESIZE - (dataLength % PAGESIZE);

            if(pinableContainer.Info != null && alignedDataLength > pinableContainer.Info.AlignedLength)
            {
                this.SetEmptyList(pinableContainer.Info);
                pinableContainer.ReleaseInfo();
            }

            if (pinableContainer.Info == null)
            {
                var emptyInfo = GetEmptyList(alignedDataLength);
                if (emptyInfo == null)
                {
                    pinableContainer.SetConent(emptyIndex, default(T), alignedDataLength);

                    writer.BaseStream.Position = emptyIndex;

                    emptyIndex += alignedDataLength;
                }
                else
                {
                    pinableContainer.SetConent(emptyInfo.Index, default(T), alignedDataLength);

                    writer.BaseStream.Position = emptyInfo.Index;
                }
            }
            else
            {
                pinableContainer.SetConent(default(T));

                writer.BaseStream.Position = pinableContainer.Info.Index;
            }

            writer.Write(data.Length);
            writer.Write(data);

            writer.Close();

            dataStream.Close();

            return;
        }
    }
}
