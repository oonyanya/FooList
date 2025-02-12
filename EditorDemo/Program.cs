// See https://aka.ms/new-console-template for more information

using FooEditEngine;
using FooProject.Collection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;

GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

Console.WriteLine("benchmark start");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

Stopwatch sw = Stopwatch.StartNew();
var buf = new StringBuffer();
for(int i = 0; i< 1000000; i++)
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
foreach(var item in buf)
{
    var _ = item;
}
sw.Stop();
Console.WriteLine(String.Format("enumratotion time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

buf.Clear();
Console.WriteLine("clear buffer");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");

Console.WriteLine("Finished.Hit Any Key");
Console.ReadLine();
