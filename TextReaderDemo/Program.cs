using CommandLine;
using FooProject.Collection;
using FooProject.Collection.DataStore;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using TextReaderDemo;
using static System.Runtime.InteropServices.JavaScript.JSType;

Console.WriteLine("open filename to show?");
string filepath = Console.ReadLine().Trim('"');

if(File.Exists(filepath) == false)
{
    Console.WriteLine("not found file");
}
else
{
    var stream = new FileStream(filepath, FileMode.Open);

    var memoryStore = new MemoryPinableContentDataStore<IComposableList<char>>();
    var lazyLoadStore = new ReadOnlyCharDataStore(stream, Encoding.UTF8);
    lazyLoadStore.SecondaryDataStore = memoryStore;
    var customConverter = new DefaultCustomConverter<char>();
    customConverter.DataStore = lazyLoadStore;
    BigList<char> biglist1 = new BigList<char>();
    biglist1.CustomBuilder = customConverter;
    biglist1.LeastFetchStore = customConverter;
    biglist1.BlockSize = 32768;

    var exitflag = false;
    while (exitflag == false)
    {
        Console.WriteLine("");
        Console.WriteLine("input command(if want to show help text,input help):");
        string cmd = Console.ReadLine();

        string[] cmds = cmd.Split(" ");
        Parser.Default.ParseArguments<LoadCommnad, LoadAllCommnad, SaveCommand, UsageCommnad, InsertCommand, RemoveCommand, ShowCommand, ExitCommnad>(cmds)
            .WithParsed<LoadCommnad>(opt => {
                for (int i = 0; i < opt.Count; i++)
                {
                    var pinableContainer = lazyLoadStore.Load(biglist1.BlockSize);
                    if (pinableContainer == null)
                        break;
                    biglist1.Add(pinableContainer);
                }
                Console.WriteLine("success to load");
            })
            .WithParsed<LoadAllCommnad>(opt => {
                while (true)
                {
                    var pinableContainer = lazyLoadStore.Load(biglist1.BlockSize);
                    if (pinableContainer == null)
                        break;
                    biglist1.Add(pinableContainer);
                }
                Console.WriteLine("success to load");
            })
            .WithParsed<SaveCommand>(opt => {
                var saveFileStream = new FileStream(opt.FilePath.Trim('"'), FileMode.OpenOrCreate);
                saveFileStream.Position = 0;
                var streamWriter = new StreamWriter(saveFileStream);
                foreach (var item in biglist1.Chunk(biglist1.BlockSize))
                {
                    streamWriter.Write(item);
                }
                Console.WriteLine("success to " + opt.FilePath);
            })
            .WithParsed<UsageCommnad>(opt => {
                Console.WriteLine("Allocated GC Memory:" + $"{System.GC.GetTotalMemory(true):N0}" + "bytes");
                Console.WriteLine("Loaded Char Count:" + biglist1.Count);
            })
            .WithParsed<InsertCommand>(opt => {
                var number = opt.Index;
                if (number >= 0 && number < biglist1.Count)
                {
                    biglist1.InsertRange(number,opt.Text);
                    Console.WriteLine("success");
                }
                else
                {
                    Console.WriteLine("too large index");
                }
            })
            .WithParsed<RemoveCommand>(opt => {
                var number = opt.Index;
                if (number >= 0 && number < biglist1.Count)
                {
                    var length = biglist1.Count;
                    if(number + length > biglist1.Count)
                    {
                        length = biglist1.Count - number;
                    }
                    biglist1.RemoveRange(number, opt.Length);
                    Console.WriteLine("success");
                }
                else
                {
                    Console.WriteLine("too large index");
                }
            })
            .WithParsed<ShowCommand>(opt => {
                Console.WriteLine("");
                var number = opt.Index;
                if (number >= 0 && number < biglist1.Count)
                {
                    int count = Math.Min(biglist1.Count - number, biglist1.BlockSize);
                    string text = new string(biglist1.GetRangeEnumerable(number, count).ToArray());
                    Console.WriteLine(text);
                }
                else
                {
                    Console.WriteLine("too large index");
                }
            })
            .WithParsed<ExitCommnad>(opt => {
                exitflag = true;
            });
    }

}
