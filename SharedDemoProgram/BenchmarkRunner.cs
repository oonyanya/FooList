using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace SharedDemoProgram
{
    public class BenchmarkRunner
    {
        const int SLEEPTIME = 2000;
        public static long Run(Action action)
        {
            if(action == null)
                throw new ArgumentNullException("action");

            try
            {
                NativeMethods.EnableConstantPower(true);
                var sw = Stopwatch.StartNew();
                action();
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            finally
            {
                NativeMethods.EnableConstantPower(false);
                System.Diagnostics.Debug.WriteLine("end prevent sleep");
            }
        }
    }

    internal static class NativeMethods
    {
        internal class PowerRequestSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private PowerRequestSafeHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(handle);
            }
        }

        #region prevent screensaver, display dimming and automatically sleeping
        static PowerRequestSafeHandle _PowerRequest; //HANDLE

        // Availability Request Functions
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern PowerRequestSafeHandle PowerCreateRequest(ref POWER_REQUEST_CONTEXT Context);

        [DllImport("kernel32.dll")]
        static extern bool PowerSetRequest(PowerRequestSafeHandle PowerRequestHandle, PowerRequestType RequestType);

        [DllImport("kernel32.dll")]
        static extern bool PowerClearRequest(PowerRequestSafeHandle PowerRequestHandle, PowerRequestType RequestType);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

        // Availablity Request Enumerations and Constants
        enum PowerRequestType
        {
            PowerRequestDisplayRequired = 0,
            PowerRequestSystemRequired,
            PowerRequestAwayModeRequired,
            PowerRequestMaximum
        }

        const int POWER_REQUEST_CONTEXT_VERSION = 0;
        const int POWER_REQUEST_CONTEXT_SIMPLE_STRING = 0x1;
        const int POWER_REQUEST_CONTEXT_DETAILED_STRING = 0x2;

        // Availablity Request Structures
        // Note:  Windows defines the POWER_REQUEST_CONTEXT structure with an
        // internal union of SimpleReasonString and Detailed information.
        // To avoid runtime interop issues, this version of 
        // POWER_REQUEST_CONTEXT only supports SimpleReasonString.  
        // To use the detailed information,
        // define the PowerCreateRequest function with the first 
        // parameter of type POWER_REQUEST_CONTEXT_DETAILED.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct POWER_REQUEST_CONTEXT
        {
            public UInt32 Version;
            public UInt32 Flags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string
                SimpleReasonString;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PowerRequestContextDetailedInformation
        {
            public IntPtr LocalizedReasonModule;
            public UInt32 LocalizedReasonId;
            public UInt32 ReasonStringCount;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string[] ReasonStrings;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct POWER_REQUEST_CONTEXT_DETAILED
        {
            public UInt32 Version;
            public UInt32 Flags;
            public PowerRequestContextDetailedInformation DetailedInformation;
        }
        #endregion



        /// <summary>
        /// Prevent screensaver, display dimming and power saving. This function wraps PInvokes on Win32 API. 
        /// </summary>
        /// <param name="enableConstantDisplayAndPower">True to get a constant display and power - False to clear the settings</param>
        public static void EnableConstantPower(bool enableConstantDisplayAndPower)
        {
            if (enableConstantDisplayAndPower)
            {
                POWER_REQUEST_CONTEXT _PowerRequestContext;
                // Set up the diagnostic string
                _PowerRequestContext.Version = POWER_REQUEST_CONTEXT_VERSION;
                _PowerRequestContext.Flags = POWER_REQUEST_CONTEXT_SIMPLE_STRING;
                _PowerRequestContext.SimpleReasonString = "Running benchmark"; // your reason for changing the power settings;

                // Create the request, get a handle
                _PowerRequest = PowerCreateRequest(ref _PowerRequestContext);

                // Set the request
                PowerSetRequest(_PowerRequest, PowerRequestType.PowerRequestSystemRequired);
                PowerSetRequest(_PowerRequest, PowerRequestType.PowerRequestAwayModeRequired);
            }
            else
            {
                // Clear the request
                PowerClearRequest(_PowerRequest, PowerRequestType.PowerRequestSystemRequired);
                PowerClearRequest(_PowerRequest, PowerRequestType.PowerRequestAwayModeRequired);

                if (_PowerRequest != null && !_PowerRequest.IsInvalid)
                    _PowerRequest.Dispose();
            }
        }
    }
}
