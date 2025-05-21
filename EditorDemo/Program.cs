// See https://aka.ms/new-console-template for more information
//ディスク上に保存するならコメントアウトする
#define DISKBASE_BUFFER

using FooEditEngine;
using FooProject.Collection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using EditorDemo;

#if DISKBASE_BUFFER
const int BENCHMARK_SIZE = 1000;
var buf = new StringBuffer(true);
#else
const int BENCHMARK_SIZE = 1000000;
var buf = new StringBuffer();
#endif

GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

Console.WriteLine("benchmark start");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

Stopwatch sw = Stopwatch.StartNew();
for(int i = 0; i< BENCHMARK_SIZE; i++)
{
    var insertStr = "this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.\n";
    buf.Replace(buf.Length, 0,insertStr,insertStr.Length);
}
sw.Stop();
Console.WriteLine(String.Format("add time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes"); 
Task.Delay(1000).Wait();

sw = Stopwatch.StartNew();
buf.ReplaceAll("pen", "cat");
sw.Stop();
Console.WriteLine(String.Format("replace 1 time:{0} ms",sw.ElapsedMilliseconds));
Task.Delay(1000).Wait();
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

sw = Stopwatch.StartNew();
buf.ReplaceAll("cat", "ratking");
sw.Stop();
Console.WriteLine(String.Format("replace 2 time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

sw = Stopwatch.StartNew();
buf.ReplaceAll("ratking", "cat");
sw.Stop();
Console.WriteLine(String.Format("replace 3 time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

sw = Stopwatch.StartNew();
//StreamWriter streamWriter = new StreamWriter("test.txt");
StreamWriter streamWriter = new StreamWriter(Stream.Null);
List<char> writeBuffer = new List<char>(4 * 1024 * 1024);
foreach(var item in buf)
{
    if(writeBuffer.Count < writeBuffer.Capacity)
    {
        writeBuffer.Add(item);
    }
    else
    {
        streamWriter.WriteLine(writeBuffer.ToArray());
        writeBuffer.Clear();
    }
}
if (writeBuffer.Count > 0)
{
    streamWriter.WriteLine(writeBuffer.ToArray());
    writeBuffer.Clear();
}
streamWriter.Close();
sw.Stop();
Console.WriteLine(String.Format("enumratotion time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

buf.Clear();
Console.WriteLine("clear buffer");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

sw = Stopwatch.StartNew();
var rangelist = new BigRangeList<LineToIndex>();
for (int i = 0; i < BENCHMARK_SIZE; i++)
{
    rangelist.Add(new LineToIndex(i + 10, 10));
}
sw.Stop();
Console.WriteLine(String.Format("add line time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

sw = Stopwatch.StartNew();
for (int i = 0; i < BENCHMARK_SIZE; i++)
{
    if(i % 100 == 0)
    {
        var data = (LineToIndex)rangelist[i].DeepCopy();
        data.length += 10;
        rangelist[i]= data;
    }
}
sw.Stop();
Console.WriteLine(String.Format("update line time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

rangelist.Clear();
Console.WriteLine("clear buffer");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

Console.WriteLine("Finished.Hit Any Key");
Console.ReadLine();
