using System;
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
        public static long Run(Action action)
        {
            if(action == null)
                throw new ArgumentNullException("action");
            try
            {
                NativeMethods.PreventSleep();
                var sw = Stopwatch.StartNew();
                action();
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            finally
            {
                NativeMethods.AllowSleep();
            }
        }
    }
    internal static class NativeMethods
    {
        public static void PreventSleep()
        {
            SetThreadExecutionState(ExecutionState.EsContinuous | ExecutionState.EsSystemRequired);
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
