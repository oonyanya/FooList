//#define KEEP_TEMPORARY_FILE
//#define USE_TWO_QUEUE_CACH
#define SAME_WRITE_AND_READ_CACHE
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
    public class DiskPinableContentDataStore<T> : PinableContentDataStoreWithAutoDisposerBase<T>
    {
        //ファイル内部の割り当ての最小単位
        const int PAGESIZE = 16384;
        //FileStreamのバッファーサイズ。
        const int BUFFERSIZE = 4096;
        
        string tempFilePath;
        ISerializeData<T> serializer;
        EmptyList emptyList = new EmptyList();
        bool disposedValue = false;
#if SAME_WRITE_AND_READ_CACHE
#if USE_TWO_QUEUE_CACHE
        ICacheList<long, PinableContainer<T>> writebackCacheList = new TwoQueueCacheList<long, PinableContainer<T>>();
#else
        ICacheList<long, PinableContainer<T>> writebackCacheList = new FIFOCacheList<long, PinableContainer<T>>();
#endif
#else
#if USE_TWO_QUEUE_CACHE
        ICacheList<long, PinableContainer<T>> readCacheList = new TwoQueueCacheList<long, PinableContainer<T>>();
#else
        ICacheList<long, PinableContainer<T>> readCacheList = new FIFOCacheList<long, PinableContainer<T>>();
#endif
        ICacheList<long, PinableContainer<T>> writebackCacheList = new FIFOCacheList<long, PinableContainer<T>>();
#endif
        BinaryWriter writer;
        BinaryReader reader;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="serializer">ISerializeDataを継承したクラスのインスタンス</param>
        /// <param name="cache_limit">キャッシュしておく量。すくなくとも、CacheParameters.MINCACHESIZE以上は指定する必要がある。</param>
        public DiskPinableContentDataStore(ISerializeData<T> serializer, int cache_limit = 128): this(serializer,null,cache_limit)
        {
        }

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="serializer">ISerializeDataを継承したクラスのインスタンス</param>
        /// <param name="stream">ストリーム。nullの場合は%TEMP%にワークファイルを作成します。</param>
        /// <param name="cache_limit">キャッシュしておく量。すくなくとも、CacheParameters.MINCACHESIZE以上は指定する必要がある。</param>
        public DiskPinableContentDataStore(ISerializeData<T> serializer, Stream stream, int cache_limit = 128)
        {
            Stream dataStream = stream;
            if (stream == null)
            {
                this.tempFilePath = Path.GetTempFileName();
                dataStream = new FileStream(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, BUFFERSIZE, FileOptions.None);
            }
            this.writer = new BinaryWriter(dataStream);
            this.reader = new BinaryReader(dataStream);
            this.serializer = serializer;
#if SAME_WRITE_AND_READ_CACHE
#else
            this.readCacheList.Limit = cache_limit;
            this.readCacheList.CacheOuted += (ev) => {

                var key = ev.Key;
                var outed_item = ev.Value;

                if (outed_item.IsRemoved == true)
                    return;

                System.Diagnostics.Debug.Assert(outed_item.Content != null);
                this.OnDispoing(outed_item.Content);

                outed_item.Info = null;
                outed_item.Content = default(T);

                this.emptyList.ReleaseID(outed_item.CacheIndex);
                outed_item.CacheIndex = PinableContainer<T>.NOTCACHED;
            };
#endif
            this.writebackCacheList.Limit = cache_limit;
            this.writebackCacheList.CacheOuted += (ev)=>{

                var key = ev.Key;
                var outed_item = ev.Value;

                if (outed_item.IsRemoved == true)
                    return;

                if (outed_item.Content == null)
                    System.Diagnostics.Debugger.Break();
                System.Diagnostics.Debug.Assert(outed_item.Content != null);
                this.OnDispoing(outed_item.Content);

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

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="serializer">ISerializeDataを継承したクラスのインスタンス</param>
        /// <param name="workfolderpath">ワークファイルを格納するフォルダーへのフルパス。nullの場合は%TEMP%を参照します。</param>
        /// <param name="cache_limit">キャッシュしておく量。すくなくとも、CacheParameters.MINCACHESIZE以上は指定する必要がある。</param>
        public static DiskPinableContentDataStore<T> Create(ISerializeData<T> serializer, string workfolderpath, int cache_limit = 128)
        {
            var tempFilePath = string.Empty;
            if (workfolderpath == null)
                tempFilePath = Path.GetTempFileName();
            else
                tempFilePath = Path.Combine(workfolderpath, Path.GetRandomFileName());
            var dataStream = new FileStream(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, BUFFERSIZE, FileOptions.None);
            var r = new DiskPinableContentDataStore<T>(serializer, dataStream, cache_limit);
            r.tempFilePath = tempFilePath;
            return r;
        }

        /// <inheritdoc/>
        public override IEnumerable<T> ForEachAvailableContent()
        {
#if SAME_WRITE_AND_READ_CACHE
#else
            foreach (var pinableContainer in this.readCacheList.ForEachValue())
            {
                if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
                {
                    yield return pinableContainer.Content;
                }
            }
#endif
            foreach (var pinableContainer in this.writebackCacheList.ForEachValue())
            {
                if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
                {
                    yield return pinableContainer.Content;
                }
            }
        }

        /// <inheritdoc/>
        public override IPinableContainer<T> CreatePinableContainer(T content)
        {
            return new PinableContainer<T>(content) { ID = nameof(DiskPinableContentDataStore<T>) };
        }


        /// <inheritdoc/>
        /// <remarks>呼び出し前にCommit()を実行すること</remarks>
        public override IPinableContainer<T> Clone(IPinableContainer<T> pin, T cloned_content)
        {
            PinableContainer<T> newpin;
            newpin = (PinableContainer<T>)this.CreatePinableContainer(cloned_content);

            PinableContainer<T> oldpin = (PinableContainer<T>)pin;
            newpin.CacheIndex = oldpin.CacheIndex;
            if(oldpin.Info != null)
            {
                newpin.Info = new DiskAllocationInfo(oldpin.Info.Index, oldpin.Info.AlignedLength);
            }
            else
            {
                newpin.Info = null;
            }
            newpin.ID = oldpin.ID;
            newpin.IsRemoved = oldpin.IsRemoved;

            System.Diagnostics.Debug.Assert(newpin.CacheIndex == oldpin.CacheIndex);
            System.Diagnostics.Debug.Assert(newpin.Info.Index == oldpin.Info.Index);
            System.Diagnostics.Debug.Assert(newpin.Info.AlignedLength == oldpin.Info.AlignedLength);
            System.Diagnostics.Debug.Assert(newpin.ID == oldpin.ID);
            System.Diagnostics.Debug.Assert(newpin.IsRemoved == oldpin.IsRemoved);

            return newpin;
        }

        /// <inheritdoc/>
        public override void Commit()
        {
#if SAME_WRITE_AND_READ_CACHE
#else
            this.readCacheList.Flush();
#endif
            this.writebackCacheList.Flush();
            this.writer.Flush();
        }

        /// <inheritdoc/>
        public override bool TryGet(IPinableContainer<T> ipinableContainer, out IPinnedContent<T> result)
        {
            var pinableContainer = (PinableContainer<T>)ipinableContainer;
            if (pinableContainer.CacheIndex != PinableContainer<T>.NOTCACHED || pinableContainer.Content?.Equals(default(T)) == false)
            {
                result = new PinnedContent<T>(pinableContainer, this);
                return true;
            }

            PinableContainer<T> _;
#if SAME_WRITE_AND_READ_CACHE
            if (this.writebackCacheList.TryGet(pinableContainer.Info.Index, out _) && pinableContainer.Content != null)
            {
                result = new PinnedContent<T>(pinableContainer, this);
            }
            else
            {
                this.reader.BaseStream.Position = pinableContainer.Info.Index;
                int count = this.reader.ReadInt32();
                var data = this.reader.ReadBytes(count);
                pinableContainer.Content = this.serializer.DeSerialize(data);
                pinableContainer.CacheIndex = this.emptyList.GetID();
                this.writebackCacheList.Set(pinableContainer.CacheIndex, pinableContainer);
                result = new PinnedContent<T>(pinableContainer, this);
            }
#else
            //キャッシュに存在してなかったら、読む
            if (this.readCacheList.TryGet(pinableContainer.Info.Index, out _) && pinableContainer.Content != null)
            {
                result = new PinnedContent<T>(pinableContainer, this);
            }
            else
            {
                this.reader.BaseStream.Position = pinableContainer.Info.Index;
                int count = this.reader.ReadInt32();
                var data = this.reader.ReadBytes(count);
                pinableContainer.Content = this.serializer.DeSerialize(data);
                pinableContainer.CacheIndex = this.emptyList.GetID();
                this.readCacheList.Set(pinableContainer.CacheIndex, pinableContainer);
                result = new PinnedContent<T>(pinableContainer, this);
            }
#endif

            return true;
        }

        /// <inheritdoc/>
        public override void Set(IPinableContainer<T> ipinableContainer)
        {
            if (EqualityComparer<T>.Default.Equals(ipinableContainer.Content, default(T)))
                return;

            var pinableContainer = (PinableContainer<T>)ipinableContainer;

            if (pinableContainer.IsRemoved)
            {
                if(pinableContainer.Info != null)
                    this.emptyList.SetEmptyList(pinableContainer.Info);
                this.emptyList.ReleaseID(pinableContainer.CacheIndex);

                System.Diagnostics.Debug.Assert(pinableContainer.Content != null);
                this.OnDispoing(pinableContainer.Content);

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

        /// <inheritdoc/>
        protected override void OnDispose(bool disposing)
        {
            try
            {
                this.writer.Dispose();
                this.reader.Dispose();
#if !KEEP_TEMPORARY_FILE
                if (this.tempFilePath != null)
                    File.Delete(this.tempFilePath);
#endif
            }
            catch
            {
                throw;
            }
        }

    }
}
