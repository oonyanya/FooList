// See https://aka.ms/new-console-template for more information

using FooEditEngine;
using FooProject.Collection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Globalization;

Console.WriteLine("benchmark start");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}");

Stopwatch sw = Stopwatch.StartNew();
var buf = new StringBuffer();
for(int i = 0; i< 1000000; i++)
{
    var insertStr = "this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.this is a pen.\n";
    buf.Replace(buf.Length, 0,insertStr,insertStr.Length);
}
sw.Stop();
Console.WriteLine(String.Format("add time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}"); 
Task.Delay(1000).Wait();

sw = Stopwatch.StartNew();
buf.ReplaceAll("pen", "cat");
sw.Stop();
Console.WriteLine(String.Format("replace time:{0} ms",sw.ElapsedMilliseconds));
Task.Delay(1000).Wait();
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}");

sw = Stopwatch.StartNew();
buf.ReplaceAll("cat", "ratking");
sw.Stop();
Console.WriteLine(String.Format("replace time:{0} ms", sw.ElapsedMilliseconds));
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}");

buf.Clear();
Console.WriteLine("clear buffer");
Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}");

Console.WriteLine("Finished.Hit Any Key");
Console.ReadLine();
