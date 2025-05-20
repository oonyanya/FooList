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
        public byte[] SerializedData { get; set; }

        public bool IsRemoved { get; set; }

        public DiskAllocationInfo()
        {
            IsRemoved = false;
        }
        public DiskAllocationInfo(long index, int length) : this()
        {
            Index = index;
            AlignedLength = length;
        }
    }

    public class DiskPinableContentDataStore<T> : IPinableContainerStore<T>, IDisposable
    {
        //ファイル内部の割り当ての最小単位
        const int PAGESIZE = 4096;
        //FileStreamのバッファーサイズ。512KBぐらいがちょうどいいようだ。https://www.cc.u-tokyo.ac.jp/public/VOL8/No5/data_no1_0609.pdf
        const int BUFFERSIZE = 512 * 1024;

        string tempFilePath;
        long emptyIndex;
        ISerializeData<T> serializer;
        EmptyList emptyList = new EmptyList();
        bool disposedValue = false;
        CacheList<long, PinableContainer<T>> cacheList = new CacheList<long, PinableContainer<T>>();

        public DiskPinableContentDataStore(ISerializeData<T> serializer,int cache_limit = 128)
        {
            tempFilePath = System.IO.Path.GetTempFileName();
            this.serializer = serializer;
            this.cacheList.Limit = cache_limit;
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

            PinableContainer<T> cached_container;
            if(this.cacheList.TryGet(pinableContainer.Info.Index, out cached_container))
            {
                pinableContainer.Content = this.serializer.DeSerialize(cached_container.Info.SerializedData);

                result = new PinnedContent<T>(pinableContainer, this);
            }
            else
            {
                using (var dataStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.None, BUFFERSIZE, FileOptions.None))
                using (var reader = new BinaryReader(dataStream))
                {
                    reader.BaseStream.Position = pinableContainer.Info.Index;

                    int count = reader.ReadInt32();

                    var data = reader.ReadBytes(count);

                    pinableContainer.Content = this.serializer.DeSerialize(data);
                    result = new PinnedContent<T>(pinableContainer, this);
                }
            }

            return true;
        }

        public void Set(PinableContainer<T> pinableContainer)
        {
            if (pinableContainer.Info != null && pinableContainer.Info.IsRemoved)
            {
                this.emptyList.SetEmptyList(pinableContainer.Info);
                pinableContainer.Info = null;
                return;
            }

            var data = this.serializer.Serialize(pinableContainer.Content);

            int dataLength = data.Length + 4;
            int alignedDataLength = dataLength + PAGESIZE - (dataLength % PAGESIZE);

            if(pinableContainer.Info != null && alignedDataLength > pinableContainer.Info.AlignedLength)
            {
                this.emptyList.SetEmptyList(pinableContainer.Info);
                pinableContainer.Info = null;
            }

            if (pinableContainer.Info == null)
            {
                var emptyInfo = this.emptyList.GetEmptyList(alignedDataLength);
                pinableContainer.Info = new DiskAllocationInfo();
                if (emptyInfo == null)
                {
                    pinableContainer.Info.Index = emptyIndex;
                    pinableContainer.Info.AlignedLength = alignedDataLength;
                    pinableContainer.Info.SerializedData = data;

                    emptyIndex += alignedDataLength;
                }
                else
                {
                    pinableContainer.Info.Index = emptyInfo.Index;
                    pinableContainer.Info.AlignedLength = alignedDataLength;
                    pinableContainer.Info.SerializedData = data;
                }
            }
            else
            {
                pinableContainer.Info.SerializedData = data;
            }

            PinableContainer<T> outed_item;
            if (this.cacheList.Set(pinableContainer.Info.Index, pinableContainer, out outed_item))
            {
                using (var dataStream = new FileStream(tempFilePath, FileMode.Open,FileAccess.Write,FileShare.None, BUFFERSIZE, FileOptions.None))
                using (var writer = new BinaryWriter(dataStream))
                {
                    writer.BaseStream.Position = outed_item.Info.Index;
                    writer.Write(outed_item.Info.SerializedData.Length);
                    writer.Write(outed_item.Info.SerializedData);
                    outed_item.Info.SerializedData = null;
                    outed_item.Content = default(T);
                }
            }

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
