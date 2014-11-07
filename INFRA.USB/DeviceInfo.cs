using System;
using System.Collections.Generic;
using System.Text;

namespace INFRA.USB
{
    /// <summary>
    /// Class containg information about a connected USB HID device
    /// </summary>
    public class DeviceInfo
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
        public int DeviceIndex { get; internal set; }

        /// <summary>
        /// Full device file path
        /// </summary>
        public string DevicePath { get; internal set; }

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
    }
}
