// See https://aka.ms/new-console-template for more information
//ディスク上に保存するならコメントアウトする
//#define DISKBASE_BUFFER
//文字列の操作の最終結果を保存するならコメントアウトする
//#define SAVE_FILE
//文字列操作の結果を各段階ごとに保存するならコメントアウトする
//#define SAVE_FILE_ALL_STAGE
//バッファーの内容をチェックするならコメントアウトする
#define VERIFY_BUFFER
//キャッシュサイズを固定するならコメントアウトする
#define FIXED_CACHE_SIZE

using FooEditEngine;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using EditorDemo;
using SharedDemoProgram;

const int BENCHMARK_SIZE = 1000000;

const string insertStr = "this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.\n";
string replacedStr = string.Empty;

Console.WriteLine("benchmark start");
Console.WriteLine("size:" + BENCHMARK_SIZE);
#if DISKBASE_BUFFER
#if FIXED_CACHE_SIZE
    var cacheSize = 128;
#else
    var twolog = (int)(Math.Log2((long) BENCHMARK_SIZE * (long)insertStr.Length  / StringBuffer.BLOCKSIZE  * 0.01) + 0.5);
    var cacheSize = (int)Math.Max(Math.Pow(2, twolog), 128);
#endif
    var buf = new StringBuffer(true, cacheSize);

    Console.WriteLine("cache size:" + cacheSize);
#else
var buf = new StringBuffer();
#endif

GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

long ElapsedMilliseconds;
ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
    for (int i = 0; i < BENCHMARK_SIZE; i++)
    {
        buf.Replace(buf.Length, 0, insertStr, insertStr.Length);
    }
});
Console.WriteLine(String.Format("add time:{0} ms", ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");
#if VERIFY_BUFFER
buf.Verify(insertStr);
#endif
#if SAVE_FILE_ALL_STAGE
buf.SaveFile("test1.txt");
#endif

ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
    buf.ReplaceAll("pen", "cat");
});
Console.WriteLine(String.Format("replace 1 time:{0} ms",ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");
#if VERIFY_BUFFER
replacedStr = insertStr.Replace("pen", "cat");
buf.Verify(replacedStr);
#endif
#if SAVE_FILE_ALL_STAGE
    buf.SaveFile("test2.txt");
#endif

ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
    buf.ReplaceAll("cat", "ratking");
});
Console.WriteLine(String.Format("replace 2 time:{0} ms", ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");
#if VERIFY_BUFFER
replacedStr = replacedStr.Replace("cat", "ratking");
buf.Verify(replacedStr);
#endif
#if SAVE_FILE_ALL_STAGE
    buf.SaveFile("test3.txt");
#endif

ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
    buf.ReplaceAll("ratking", "cat");
});
Console.WriteLine(String.Format("replace 3 time:{0} ms", ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");
#if VERIFY_BUFFER
replacedStr = replacedStr.Replace("ratking", "cat");
buf.Verify(replacedStr);
#endif
#if SAVE_FILE_ALL_STAGE
    buf.SaveFile("test4.txt");
#endif

ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
#if SAVE_FILE || SAVE_FILE_ALL_STAGE
    buf.SaveFile("test_last.txt");
#else
    buf.SaveFile(null);
#endif
});
Console.WriteLine(String.Format("enumratotion time:{0} ms", ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

buf.Clear();
Console.WriteLine("clear buffer");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

var rangelist = new BigRangeList<LineToIndex>();
#if DISKBASE_BUFFER
    var serializer = new LineToIndexTableSerializer();
    var dataStore = new DiskPinableContentDataStore<IComposableList<LineToIndex>>(serializer);
#else
var dataStore = new MemoryPinableContentDataStore<IComposableList<LineToIndex>>();
#endif
rangelist.CustomBuilder.DataStore = dataStore;

ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
    for (int i = 0; i < BENCHMARK_SIZE; i++)
    {
        rangelist.Add(new LineToIndex(i + 10, 10));
    }
});
Console.WriteLine(String.Format("add line time:{0} ms", ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
    for (int i = 0; i < BENCHMARK_SIZE; i++)
    {
        if (i % 100 == 0)
        {
            var data = (LineToIndex)rangelist[i].DeepCopy();
            data.length += 10;
            rangelist[i] = data;
        }
    }
});
Console.WriteLine(String.Format("update line time(per 100 lines):{0} ms", ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
    for (int i = 0; i < BENCHMARK_SIZE; i++)
    {
        var row = rangelist.GetIndexFromAbsoluteIndexIntoRange(i * 10 + 1);
    }
});
Console.WriteLine(String.Format("convert index to line time:{0} ms", ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

ElapsedMilliseconds = BenchmarkRunner.Run(() =>
{
    for (int i = 0; i < BENCHMARK_SIZE; i++)
    {
        var index = rangelist.GetWithConvertAbsolteIndex(i);
    }
});
Console.WriteLine(String.Format("convert line to index time:{0} ms", ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

rangelist.Clear();
Console.WriteLine("clear buffer");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

Console.WriteLine("Finished.Hit Any Key");
Console.ReadLine();
