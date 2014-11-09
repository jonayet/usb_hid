using System;

namespace INFRA.USB.Classes
{
    internal static class Constants
    {
        /// <summary>Used when registering for device insert/remove messages : we're giving the API call a window handle</summary>
        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
        /// <summary>Purges Win32 transmit buffer by aborting the current transmission.</summary>
        public const uint PURGE_TXABORT = 0x01;
        /// <summary>Purges Win32 receive buffer by aborting the current receive.</summary>
        public const uint PURGE_RXABORT = 0x02;
        /// <summary>Purges Win32 transmit buffer by clearing it.</summary>
        public const uint PURGE_TXCLEAR = 0x04;
        /// <summary>Purges Win32 receive buffer by clearing it.</summary>
        public const uint PURGE_RXCLEAR = 0x08;
        /// <summary>CreateFile : Resource will be "created" or existing will be used</summary>
        public const uint OPEN_ALWAYS = 4;
        /// <summary>ReadFile/WriteFile : Overlapped operation is incomplete.</summary>
        public const uint ERROR_IO_PENDING = 997;
        /// <summary>Infinite timeout</summary>
        public const uint INFINITE = 0xFFFFFFFF;
        /// <summary>Simple representation of a null handle : a closed stream will get this handle. Note it is public for comparison by higher level classes.</summary>
        public static IntPtr NullHandle = IntPtr.Zero;
        public static IntPtr InvalidHandle = new IntPtr(-1);

        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_DEVICEINTERFACE = 0x00000010;
        public const int DEVTYP_DEVICEINTERFACE = 0x00000005;
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const int FILE_SHARE_READ = 0x00000001;
        public const int FILE_SHARE_WRITE = 0x00000002;
        public const int OPEN_EXISTING = 3;
        public const int EvRxflag = 0x0002;    // received certain character

        // specified in DCB
        public const int InvalidHandleValue = -1;
        public const int ErrorInvalidHandle = 6;
        public const uint FileFlagOverlaped = 0x40000000;

        // Api Constatnts
        public const int FILE_FLAG_OVERLAPPED = 0x40000000;
        public const int WAIT_TIMEOUT = 0x102;
        public const short WAIT_OBJECT_0 = 0;

        // Typedef enum defines a set of integer constants for HidP_Report_Type
        public const short HidPInput = 0;
        public const short HidPOutput = 1;
        public const short HidPFeature = 2;

        // from dbt.h
        internal const Int32 WM_DEVICECHANGE = 0x219;
        internal const Int32 DEVICE_ARRIVAL = 0x8000;
        internal const Int32 DEVICE_REMOVECOMPLETE = 0x8004;
        internal const Int32 DbtDevtypDeviceinterface = 5;
        internal const Int32 DbtDevtypHandle = 6;
        internal const Int32 DbtDevnodesChanged = 7;
        internal const Int32 DeviceNotifyAllInterfaceClasses = 4;
        internal const Int32 DeviceNotifyServiceHandle = 1;
        internal const Int32 DeviceNotifyWindowHandle = 0;
    }
}
