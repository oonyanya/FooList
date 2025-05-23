//#define KEEP_TEMPORARY_FILE
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

        public DiskAllocationInfo()
        {
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
        const int PAGESIZE = 16384;
        //FileStreamのバッファーサイズ。
        const int BUFFERSIZE = 4096;

        string tempFilePath;
        ISerializeData<T> serializer;
        EmptyList emptyList = new EmptyList();
        bool disposedValue = false;
        CacheList<long, PinableContainer<T>> writebackCacheList = new CacheList<long, PinableContainer<T>>();

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="serializer">ISerializeDataを継承したクラスのインスタンス</param>
        /// <param name="cache_limit">キャッシュしておく量。あまり小さくするとGetを連続で呼んだときにエラーを吐くことがあります。</param>
        public DiskPinableContentDataStore(ISerializeData<T> serializer,int cache_limit = 128)
        {
            //LeafNodeで行う操作の関係で２以上にしないと落ちることがある
            if (cache_limit < 2)
                throw new ArgumentOutOfRangeException("cache_limit must be grater than 1");

            tempFilePath = System.IO.Path.GetTempFileName();
            this.serializer = serializer;
            this.writebackCacheList.Limit = cache_limit;
            this.writebackCacheList.CacheOuted = new Action<long, PinableContainer<T>>( (key, outed_item)=>{
                if (outed_item.IsRemoved == true)
                    return;

                var data = this.serializer.Serialize(outed_item.Content);

                int dataLength = data.Length + 4;
                int alignedDataLength = dataLength + PAGESIZE - (dataLength % PAGESIZE);

                if (outed_item.Info != null && alignedDataLength > outed_item.Info.AlignedLength)
                {
                    this.emptyList.SetEmptyList(outed_item.Info);
                    outed_item.Info = null;
                }

                if (outed_item.Info == null)
                {
                    outed_item.Info = this.emptyList.GetEmptyList(alignedDataLength);
                }

                using (var dataStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Write, FileShare.None, BUFFERSIZE, FileOptions.None))
                using (var writer = new BinaryWriter(dataStream))
                {
                    writer.BaseStream.Position = outed_item.Info.Index;
                    writer.Write(data.Length);
                    writer.Write(data);
                    outed_item.Content = default(T);
                    this.emptyList.SetID(outed_item.CacheIndex);
                    outed_item.CacheIndex = PinableContainer<T>.NOTCACHED;
                }
            });
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
            if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
            {
                result = new PinnedContent<T>(pinableContainer, this);
                return true;
            }

            PinableContainer<T> _;
            //キャッシュに存在してなかったら、読む
            if(this.writebackCacheList.TryGet(pinableContainer.Info.Index, out _) && pinableContainer.Content != null)
            {
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
            if (pinableContainer.IsRemoved)
            {
                if(pinableContainer.Info != null)
                    this.emptyList.SetEmptyList(pinableContainer.Info);
                this.emptyList.SetID(pinableContainer.CacheIndex);
                pinableContainer.Info = null;
                pinableContainer.Content = default(T);
                pinableContainer.CacheIndex = PinableContainer<T>.NOTCACHED;
                return;
            }

            if (pinableContainer.CacheIndex == PinableContainer<T>.NOTCACHED)
            {
                pinableContainer.CacheIndex = this.emptyList.GetID();
            }

            this.writebackCacheList.Set(pinableContainer.CacheIndex, pinableContainer);
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
#if KEEP_TEMPORARY_FILE
                File.Delete(this.tempFilePath);
#endif
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
