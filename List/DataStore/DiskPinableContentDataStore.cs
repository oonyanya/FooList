//#define KEEP_TEMPORARY_FILE
//#define USE_TWO_QUEUE_CACHE
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

    /// <summary>
    /// ディスクに格納するタイプのデーターストアです
    /// </summary>
    /// <typeparam name="T">データーストアに納める型を指定する</typeparam>
    public class DiskPinableContentDataStore<T> : IPinableContainerStoreWithAutoDisposer<T>, IDisposable
    {
        //ファイル内部の割り当ての最小単位
        const int PAGESIZE = 16384;
        //FileStreamのバッファーサイズ。
        const int BUFFERSIZE = 4096;

        string tempFilePath;
        ISerializeData<T> serializer;
        EmptyList emptyList = new EmptyList();
        bool disposedValue = false;
#if USE_TWO_QUEUE_CACHE
        ICacheList<long, PinableContainer<T>> writebackCacheList = new TwoQueueCacheList<long, PinableContainer<T>>();
#else
        ICacheList<long, PinableContainer<T>> writebackCacheList = new FIFOCacheList<long, PinableContainer<T>>();
#endif
        BinaryWriter writer;
        BinaryReader reader;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="serializer">ISerializeDataを継承したクラスのインスタンス</param>
        /// <param name="cache_limit">キャッシュしておく量。少なくとも２以上は指定する必要があります</param>
        public DiskPinableContentDataStore(ISerializeData<T> serializer, int cache_limit = 128): this(serializer,null,cache_limit)
        {
        }

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="serializer">ISerializeDataを継承したクラスのインスタンス</param>
        /// <param name="workfolderpath">ワークファイルを格納するフォルダーへのフルパス。nullの場合は%TEMP%を参照します。</param>
        /// <param name="cache_limit">キャッシュしておく量。少なくとも４以上は指定する必要があります</param>
        public DiskPinableContentDataStore(ISerializeData<T> serializer,string workfolderpath,int cache_limit = 128)
        {
            if (workfolderpath == null)
                tempFilePath = Path.GetTempFileName();
            else
                tempFilePath = Path.Combine(workfolderpath,Path.GetRandomFileName());
            var dataStream = new FileStream(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, BUFFERSIZE, FileOptions.None);
            this.writer = new BinaryWriter(dataStream);
            this.reader = new BinaryReader(dataStream);
            this.serializer = serializer;
            this.writebackCacheList.Limit = cache_limit;
            this.writebackCacheList.CacheOuted += (ev)=>{

                var key = ev.Key;
                var outed_item = ev.Value;
                this.OnDispoing(outed_item.Content);

                if (outed_item.IsRemoved == true)
                    return;

                if (ev.RequireWriteBack)
                {
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

                    this.writer.BaseStream.Position = outed_item.Info.Index;
                    this.writer.Write(data.Length);
                    this.writer.Write(data);
                    outed_item.Content = default(T);
                }
                this.emptyList.ReleaseID(outed_item.CacheIndex);
                outed_item.CacheIndex = PinableContainer<T>.NOTCACHED;
            };
        }

        public event Action<T> Disposeing;

        public void OnDispoing(T item)
        {
            if (this.Disposeing != null)
                this.Disposeing(item);
        }

        public IEnumerable<T> ForEachAvailableContent()
        {
            foreach(var pinableContainer in this.writebackCacheList.ForEachValue())
            {
                if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
                {
                    yield return pinableContainer.Content;
                }
            }
        }

        public void Commit()
        {
            this.writebackCacheList.Flush();
            this.writer.Flush();
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
                this.reader.BaseStream.Position = pinableContainer.Info.Index;
                int count = this.reader.ReadInt32();
                var data = this.reader.ReadBytes(count);
                pinableContainer.Content = this.serializer.DeSerialize(data);
                result = new PinnedContent<T>(pinableContainer, this);
            }

            return true;
        }

        public void Set(PinableContainer<T> pinableContainer)
        {
            if (pinableContainer.IsRemoved)
            {
                if(pinableContainer.Info != null)
                    this.emptyList.SetEmptyList(pinableContainer.Info);
                this.emptyList.ReleaseID(pinableContainer.CacheIndex);
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
                this.writer.Dispose();
                this.reader.Dispose();
#if !KEEP_TEMPORARY_FILE
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
