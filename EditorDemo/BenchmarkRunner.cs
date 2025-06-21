using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EditorDemo
{
    public class BenchmarkRunner
    {
        const int SLEEPTIME = 2000;
        private static CancellationTokenSource _tokenSource = null;
        private static BlockingCollection<bool> _queque = new BlockingCollection<bool>();
        public static long Run(Action action)
        {
            if(action == null)
                throw new ArgumentNullException("action");

            _tokenSource = new CancellationTokenSource();
            // Windows11以降、定期的に呼ばないとスリープできないらしいので、マネする
            // https://github.com/microsoft/PowerToys/blob/main/src/modules/awake/Awake/Core/Manager.cs
            Thread monitorThread = new Thread(() => {
                System.Diagnostics.Debug.WriteLine("begin prevent sleep");
                while (true)
                {
                    var state = _queque.Take();
                    if (state)
                    {
                        NativeMethods.PreventSleep();
                    }
                    else
                    {
                        NativeMethods.AllowSleep();
                        break;
                    }
                }
            });
            monitorThread.Start();

            _queque.Add(true);

            try
            {
                var sw = Stopwatch.StartNew();
                action();
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            finally
            {
                //スレッドを止めて、スリープできるようにする
                _queque.Add(false);
                Task.Delay(SLEEPTIME).Wait();
                System.Diagnostics.Debug.WriteLine("end prevent sleep");
            }
        }
    }
    internal static class NativeMethods
    {
        public static void PreventSleep()
        {
            SetThreadExecutionState(ExecutionState.EsContinuous | ExecutionState.EsSystemRequired | ExecutionState.EsAwaymodeRequired);
        }

        public static void AllowSleep()
        {
            SetThreadExecutionState(ExecutionState.EsContinuous);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        [FlagsAttribute]
        private enum ExecutionState : uint
        {
            EsAwaymodeRequired = 0x00000040,
            EsContinuous = 0x80000000,
            EsDisplayRequired = 0x00000002,
            EsSystemRequired = 0x00000001
        }
    }
}
