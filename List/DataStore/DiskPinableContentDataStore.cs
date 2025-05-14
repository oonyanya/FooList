using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    public class DiskPinableContentDataStore<T> : IPinableContainerStore<T>
    {
        const int PAGESIZE = 32768;

        string tempFilePath;
        long emptyIndex;
        ISerializeData<T> serializer;

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
            if (pinableContainer.Index == -1 || pinableContainer.Content?.Equals(default(T)) == false)
            {
                result = new PinnedContent<T>(pinableContainer, this);
                return true;
            }

            var dataStream = File.Open(tempFilePath, FileMode.Open);

            var reader = new BinaryReader(dataStream);

            reader.BaseStream.Position = pinableContainer.Index;

            int count = reader.ReadInt32();

            var data = reader.ReadBytes(count);

            pinableContainer.SetConent(pinableContainer.Index, this.serializer.DeSerialize(data));

            result = new PinnedContent<T>(pinableContainer, this);

            reader.Close();

            dataStream.Close();

            return true;
        }

        public void Set(PinableContainer<T> pinableContainer)
        {
            var dataStream = File.Open(tempFilePath, FileMode.Open);

            var writer = new BinaryWriter(dataStream);

            var data = this.serializer.Serialize(pinableContainer.Content);

            if (pinableContainer.Index == -1)
            {
                pinableContainer.SetConent(emptyIndex, default(T));
                writer.BaseStream.Position = emptyIndex;
                long dataLength = data.Length + 4;
                long alignedDataLength = dataLength + PAGESIZE - (dataLength % PAGESIZE);
                emptyIndex += alignedDataLength;
            }
            else
            {
                pinableContainer.SetConent(pinableContainer.Index, default(T));
                writer.BaseStream.Position = pinableContainer.Index;
            }
            writer.Write(data.Length);
            writer.Write(data);

            writer.Close();

            dataStream.Close();

            return;
        }
    }
}
