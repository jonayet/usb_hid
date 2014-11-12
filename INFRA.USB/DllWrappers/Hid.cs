using System;
using System.Runtime.InteropServices;
using System.Text;
using INFRA.USB.Classes;
using Microsoft.Win32.SafeHandles;

namespace INFRA.USB.DllWrappers
{
    internal static class Hid
    {
        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_FlushQueue(SafeFileHandle hidDeviceObject);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetAttributes(SafeFileHandle hidDeviceObject, ref Structures.HidAttributes attributes);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetFeature(SafeFileHandle hidDeviceObject, byte[] lpReportBuffer, Int32 reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetInputReport(SafeFileHandle hidDeviceObject, byte[] lpReportBuffer, Int32 reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetNumInputBuffers(SafeFileHandle hidDeviceObject, ref Int32 numberBuffers);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, ref IntPtr preparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_SetFeature(SafeFileHandle hidDeviceObject, byte[] lpReportBuffer, Int32 reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_SetNumInputBuffers(SafeFileHandle hidDeviceObject, Int32 numberBuffers);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_SetOutputReport(SafeFileHandle hidDeviceObject, byte[] lpReportBuffer, Int32 reportBufferLength);
     
        [DllImport("hid.dll", SetLastError = true)]
        public static extern Int32 HidP_GetCaps(IntPtr preparsedData, out Structures.HidCaps capabilities);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern Int32 HidP_GetValueCaps(Int32 reportType, byte[] valueCaps, ref Int32 valueCapsLength, IntPtr preparsedData);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean HidD_GetManufacturerString(SafeFileHandle hFile, StringBuilder buffer, Int32 bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean HidD_GetProductString(SafeFileHandle hFile, StringBuilder buffer, Int32 bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean HidD_GetSerialNumberString(SafeFileHandle hDevice, StringBuilder buffer, Int32 bufferLength);

        /// <summary>
        /// Helper to get the HID guid.
        /// </summary>
        public static Guid HIDGuid
        {
            get
            {
                Guid gHid;
                HidD_GetHidGuid(out gHid);
                return gHid;
                //return new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); //gHid;
            }
        }
   }
}
