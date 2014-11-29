using System;
using System.Runtime.InteropServices;
using INFRA.USB.HidHelper;

namespace INFRA.USB.DllWrappers
{
    internal static class SetupApi
    {
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern Int32 SetupDiCreateDeviceInfoList(ref Guid classGuid, Int32 hwndParent);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern Int32 SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet); 

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(ref Guid lpHidGuid, IntPtr enumerator, IntPtr hwndParent, Int32 flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid hidGuid, Int32 memberIndex, ref Structures.SpDeviceInterfaceData deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref Structures.SpDeviceInterfaceData deviceInterfaceData, ref Structures.SpDeviceInterfaceDetailData deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, IntPtr requiredSize, IntPtr deviceInfoData);
    }
}
