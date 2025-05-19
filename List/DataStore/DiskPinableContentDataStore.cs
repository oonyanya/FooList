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

    public class DiskPinableContentDataStore<T> : IPinableContainerStore<T>, IDisposable
    {
        const int PAGESIZE = 32768;

        string tempFilePath;
        long emptyIndex;
        ISerializeData<T> serializer;
        EmptyList emptyList = new EmptyList();
        bool disposedValue = false;


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

        public void Set(PinableContainer<T> pinableContainer)
        {
            if (pinableContainer.Content == null)
            {
                this.emptyList.SetEmptyList(pinableContainer.Info);
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
                this.emptyList.SetEmptyList(pinableContainer.Info);
                pinableContainer.ReleaseInfo();
            }

            if (pinableContainer.Info == null)
            {
                var emptyInfo = this.emptyList.GetEmptyList(alignedDataLength);
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

        public void Dispose()
        {
            //GC前にプログラム的にリソースを破棄するので
            //管理,非管理リソース両方が破棄されるようにする
            Dispose(true);
            GC.SuppressFinalize(this);//破棄処理は完了しているのでGC不要の合図
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            if (disposing)
            {
                //管理リソースの破棄処理
            }

            //非管理リソースの破棄処理
            try
            {
                File.Delete(this.tempFilePath);
            }
            catch
            {
                throw;
            }

            disposedValue = true;
        }

        ~DiskPinableContentDataStore()
        {
            //GC時に実行されるデストラクタでは非管理リソースの削除のみ
            Dispose(false);
        }
    }
}
