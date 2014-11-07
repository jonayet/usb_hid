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
    internal class HidDevice : BasicHidDevice
    {
        #region Public Fields
        /// <summary>Device Index</summary>
        public int DeviceIndex { get; private set; }

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
                    ProductVersion = this.ProductVersion.ToString(),
                };
                return devInfo;
            }
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Initialize device with given path string.
        /// </summary>
        /// <param name="devicePath"></param>
        public HidDevice(string devicePath)
        {
            DeviceIndex = 0;
            Initialize(devicePath);
        }

        /// <summary>
        /// Finds a device given its PID and VID
        /// </summary>
        /// <param name="vendorId">Vendor id for device (VID)</param>
        /// <param name="productId">Product id for device (PID)</param>
        /// <param name="index">Adress index if more than one device found.</param>
        public HidDevice(ushort vendorId, ushort productId, int index = 0)
        {
            DeviceIndex = index;

            List<string> devicePathList = new List<string>();
            
            // first, build the path search string
            string strSearch = string.Format("vid_{0:x4}&pid_{1:x4}", vendorId, productId);

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
                    if (strDevicePath != null && strDevicePath.Contains(strSearch))
                    {
                        devicePathList.Add(strDevicePath);
                    }
                }

                // initialise it with the device path
                if (devicePathList.Count > index)
                {
                    Initialize(devicePathList[index]);
                }
            }
            catch (Exception ex)
            {
                //throw HIDDeviceException.GenerateError(ex.ToString());
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                // Before we go, we have to free up the InfoSet memory reserved by SetupDiGetClassDevs
                SetupDiDestroyDeviceInfoList(hInfoSet);
            }
        }
        
        #endregion

        #region Public Methods
        public void CheckDevice()
        {
            Initialize(DevicePath);
        } 
        #endregion

        #region Private helper methods
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
            DataRecievedEventHandler handler;
            lock (DataReceived) { handler = DataReceived; }
            if (handler == null) return;
            var reportData = new byte[MaxOutputReportLength - 1];
            Array.Copy(report.Buffer, 1, reportData, 0, reportData.Length);
            handler(this, new DataRecievedEventArgs(reportData));
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
