using System;
using System.Runtime.InteropServices;

namespace INFRA.USB.HelperClasses
{
	/// <summary>
	/// Class that wraps USB API calls and structures.
	/// </summary>
    internal class Structures
    {
        /// <summary>
        /// An overlapped structure used for overlapped IO operations. The structure is
        /// only used by the OS to keep state on pending operations. You don't need to fill anything in if you
        /// unless you want a Windows event to fire when the operation is complete.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Overlapped
        {
            public uint Internal;
            public uint InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr Event;
        }

		/// <summary>
		/// Provides the capabilities of a HID device
		/// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct HidCaps
        {
            public short Usage;
            public short UsagePage;
            public short InputReportByteLength;
            public short OutputReportByteLength;
            public short FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public short[] Reserved;
            public short NumberLinkCollectionNodes;
            public short NumberInputButtonCaps;
            public short NumberInputValueCaps;
            public short NumberInputDataIndices;
            public short NumberOutputButtonCaps;
            public short NumberOutputValueCaps;
            public short NumberOutputDataIndices;
            public short NumberFeatureButtonCaps;
            public short NumberFeatureValueCaps;
            public short NumberFeatureDataIndices;
        }

        /// <summary>
        /// Provides details about a single USB device
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SpDeviceInterfaceData
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public int Reserved;
        }

		/// <summary>
		/// Access to the path for a device
		/// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SpDeviceInterfaceDetailData
        {
            public int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

		/// <summary>
		/// Used when registering a window to receive messages about devices added or removed from the system.
		/// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        public class DeviceBroadcastInterface
        {
            public int Size;
            public int DeviceType;
            public int Reserved;
            public Guid ClassGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        public class DevBroadcastDeviceinterface
        {
            internal Int32 dbcc_size;
            internal Int32 dbcc_devicetype;
            internal Int32 dbcc_reserved;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            internal Byte[] dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            internal Char[] dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DevBroadcastHdr
        {
            internal Int32 dbch_size;
            internal Int32 dbch_devicetype;
            internal Int32 dbch_reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HidAttributes
        {
            public Int32 Size;
            public Int16 VendorID;
            public Int16 ProductID;
            public Int16 VersionNumber;
        }
    }
}
