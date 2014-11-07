using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace INFRA.USB
{
    /// <summary>
    /// Class containg information about a connected USB HID device
    /// </summary>
    public class HidDevice
    {
        /// <summary>
        /// Vendor ID
        /// </summary>
        public ushort VendorID { get; internal set; }

        /// <summary>
        /// Product ID
        /// </summary>
        public ushort ProductID { get; internal set; }

        /// <summary>
        /// Device Index
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// Full device file path
        /// </summary>
        public string PathString { get; internal set; }

        /// <summary>
        /// Maximum Input Report Length
        /// </summary>
        public int MaxInputReportLength { get; internal set; }

        /// <summary>
        /// Maximum Output Report Length
        /// </summary>
        public int MaxOutputReportLength { get; internal set; }

        /// <summary>
        /// Product Name
        /// </summary>
        public string ProductName { get; internal set; }

        /// <summary>
        /// Serial Number
        /// </summary>
        public string SerialNumber { get; internal set; }

        /// <summary>
        /// Product Version
        /// </summary>
        public string ProductVersion { get; internal set; }

        /// <summary>
        /// Manufacturer
        /// </summary>
        public string Manufacturer { get; internal set; }

        /// <summary>
        /// Handle used for communicating via hid.dll
        /// </summary>
        public SafeFileHandle HidHandle { get; internal set; }

        /// <summary>
        /// Read handle from the device
        /// </summary>
        public SafeFileHandle ReadHandle { get; internal set; }

        /// <summary>
        /// Write handle to the device
        /// </summary>
        public SafeFileHandle WriteHandle { get; internal set; }

        /// <summary>
        /// Device attached flag
        /// </summary>
        public bool IsAttached { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public HidDevice()
        {
            VendorID = 0;
            ProductID = 0;
            Index = 0;
            PathString = "";
            MaxInputReportLength = 0;
            MaxOutputReportLength = 0;
            ProductName = "";
            SerialNumber = "";
            ProductVersion = "";
            Manufacturer = "";
            HidHandle = null;
            ReadHandle = null;
            WriteHandle = null;
        }
    }
}
