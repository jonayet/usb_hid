using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace INFRA.USB
{
    internal class HidDevice : HidCommunication
    {
        #region Public Fields
        /// <summary>Vendor ID</summary>
        public ushort VendorID { get; set; }

        /// <summary>Product ID</summary>
        public ushort ProductID { get; set; }

        /// <summary>Device Index</summary>
        public int DeviceIndex { get; set; }

        /// <summary>File path of the device</summary>
        public string DevicePath { get; private set; }

        /// <summary>IsConnected</summary>
        public bool IsConnected { get; private set; }

        /// <summary>Details Device Information</summary>
        public DeviceInfo DeviceInfo
        {
            get
            {
                var stringBuilder = new StringBuilder();
                var devInfo = new DeviceInfo()
                {
                    VendorID = this.VendorID,
                    ProductID = this.ProductID,
                    DeviceIndex = this.DeviceIndex,
                    DevicePath = this.DevicePath,
                    Manufacturer = HidD_GetManufacturerString(DeviceHandle, stringBuilder, 256) ? stringBuilder.ToString() : "",
                    ProductName = HidD_GetProductString(DeviceHandle, stringBuilder, 256) ? stringBuilder.ToString() : "",
                    SerialNumber = HidD_GetSerialNumberString(DeviceHandle, stringBuilder, 256) ? stringBuilder.ToString() : "",
                    MaxInputReportLength = this.MaxInputReportLength,
                    MaxOutputReportLength = this.MaxOutputReportLength,
                    ProductVersion = _productVersion.ToString(),
                };
                return devInfo;
            }
        }

        #endregion

        private int _productVersion;

        #region Constructors
        /// <summary>
        /// Initialize device with given path string.
        /// </summary>
        /// <param name="devicePath"></param>
        public HidDevice(string devicePath)
        {
            VendorID = 0;
            ProductID = 0;
            DeviceIndex = 0;
            DevicePath = devicePath;

            if(FindDevice())
            {
                Open(DevicePath);
            }
        }

        /// <summary>
        /// Finds a device given its PID and VID
        /// </summary>
        /// <param name="vendorId">Vendor id for device (VID)</param>
        /// <param name="productId">Product id for device (PID)</param>
        /// <param name="index">Adress index if more than one device found.</param>
        public HidDevice(ushort vendorId, ushort productId, int index = 0)
        {
            VendorID = vendorId;
            ProductID = productId;
            DeviceIndex = index;

            if (FindDevice())
            {
                Open(DevicePath);
            }
        }
        
        #endregion

        #region Public Methods
        public bool FindDevice()
        {
            var devicePathList = new List<string>();
            IsConnected = false;

            // first, build the path search string
            string searchText;
            if (VendorID == 0 && ProductID == 0 && DeviceIndex == 0)
            {
                searchText = DevicePath;
            }
            else
            {
                searchText = string.Format("vid_{0:x4}&pid_{1:x4}", VendorID, ProductID);   
            }
            
            // next, get the GUID from Windows that it uses to represent the HID USB interface
            Guid gHid = HIDGuid;

            // this gets a list of all HID devices currently connected to the computer (InfoSet)
            IntPtr hInfoSet = SetupDiGetClassDevs(ref gHid, null, IntPtr.Zero, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);

            try
            {
                // build up a device interface data block
                var oInterface = new DeviceInterfaceData();
                oInterface.Size = Marshal.SizeOf(oInterface);

                // Now iterate through the InfoSet memory block assigned within Windows in the call to SetupDiGetClassDevs
                // to get device details for each device connected
                for (int i = 0; SetupDiEnumDeviceInterfaces(hInfoSet, 0, ref gHid, (uint)i, ref oInterface); i++)
                {
                    // get the device path (see helper method 'GetDevicePath')
                    string strDevicePath = GetDevicePath(hInfoSet, ref oInterface);

                    // do a string search, if we find the VID/PID string then we found our device!
                    if (strDevicePath != null && strDevicePath.Contains(searchText))
                    {
                        devicePathList.Add(strDevicePath);
                    }
                }

                // initialise it with the device path
                if (devicePathList.Count > DeviceIndex)
                {
                    if (!string.IsNullOrEmpty(devicePathList[DeviceIndex]))
                    {
                        DevicePath = devicePathList[DeviceIndex];
                        IsConnected = true;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hInfoSet);
            }
            return false;
        } 
        #endregion

        #region Private helper methods

        private bool GetAttributes()
        {
            var attributes = new HIDD_ATTRIBUTES();
            attributes.Size = Marshal.SizeOf(attributes);
            if (!HidD_GetAttributes(DeviceHandle, ref attributes)) { return false; }
            _productVersion = attributes.VersionNumber;
            return true;
        }

        /// <summary>
        /// Helper method to return the device path given a DeviceInterfaceData structure and an InfoSet handle.
        /// Used in 'FindDevice' so check that method out to see how to get an InfoSet handle and a DeviceInterfaceData.
        /// </summary>
        /// <param name="hInfoSet">Handle to the InfoSet</param>
        /// <param name="oInterface">DeviceInterfaceData structure</param>
        /// <returns>The device path or null if there was some problem</returns>
        private string GetDevicePath(IntPtr hInfoSet, ref DeviceInterfaceData oInterface)
        {
            uint nRequiredSize = 0;
            if (!SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, IntPtr.Zero, 0, ref nRequiredSize, IntPtr.Zero))
            {
                var size = IntPtr.Size == 8 ? 8 : (4 + Marshal.SystemDefaultCharSize);
                var oDetail = new DeviceInterfaceDetailData { Size = 5 }; // hardcoded to 5
                if (SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, ref oDetail, nRequiredSize, ref nRequiredSize, IntPtr.Zero))
                {
                    return oDetail.DevicePath;
                }
            }
            return null;
        }
        #endregion

        #region Event properties
        /// <summary>
        /// Event handler called after Data sent complete.
        /// </summary>
        internal event DataSentEventHandler DataSent;

        /// <summary>
        /// Event handler called when new data received.
        /// </summary>
        internal event DataRecievedEventHandler DataReceived;

        /// <summary>
        /// Event handler called when new Serial received.
        /// </summary>
        internal event SerialPacketRecievedEventHandler SerialPacketRecieved; 
        #endregion

        #region Overriden Methods
        protected override void OnDataSent(OutputReport report)
        {
            DataSentEventHandler handler;
            lock (DataSent) { handler = DataSent; }
            if (handler != null)
            {
                var reportData = new byte[MaxOutputReportLength - 1];
                Array.Copy(report.Buffer, 1, reportData, 0, reportData.Length);
                handler(this, new DataSentEventArgs(reportData));
            }
        }

        protected override void OnDataReceived(InputReport report)
        {
            // Fire the event handler if assigned
            if (DataReceived == null) return;
            var reportData = new byte[MaxOutputReportLength - 1];
            Array.Copy(report.Buffer, 1, reportData, 0, reportData.Length);
            DataReceived(this, new DataRecievedEventArgs(reportData));
        } 
        #endregion

        #region Disposal methods
        protected override void Dispose(bool bDisposing)
        {
            if (bDisposing)
            {
                // to do's before exit
            }
            base.Dispose(bDisposing);
        } 
        #endregion
    }
}
