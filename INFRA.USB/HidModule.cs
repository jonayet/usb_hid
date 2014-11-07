using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Text;
using UsbHid.USB.Classes;

namespace INFRA.USB
{
    internal class HidModule : HidCommunication
    {
        #region Public Fields
        /// <summary>Details Device Information</summary>
        public HidDevice HidDevice
        {
            get
            {
                var stringBuilder = new StringBuilder();
                var devInfo = new HidDevice()
                {
                    VendorID = _hidDevice.VendorID,
                    ProductID = _hidDevice.ProductID,
                    Index = _hidDevice.Index,
                    PathString = _hidDevice.PathString,
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

        #region private fields
        private HidDevice _hidDevice;
        private HidDiscovery _hidDiscovery;
        private int _productVersion;
        #endregion

        #region Constructors
        /// <summary>
        /// Initialize device with given path string.
        /// </summary>
        /// <param name="devicePath"></param>
        public HidModule(string devicePath)
        {
            _hidDevice = new HidDevice {PathString = devicePath};
            _hidDiscovery = new HidDiscovery(ref _hidDevice);
            if (_hidDiscovery.FindTargetDevice())
            {
                //Open(DevicePath);
            }
        }

        /// <summary>
        /// Finds a device given its PID and VID
        /// </summary>
        /// <param name="vendorId">Vendor id for device (VID)</param>
        /// <param name="productId">Product id for device (PID)</param>
        /// <param name="index">Adress index if more than one device found.</param>
        public HidModule(ushort vendorId, ushort productId, int index = 0)
        {
            _hidDevice = new HidDevice {VendorID = vendorId, ProductID = productId, Index = index};
            _hidDiscovery = new HidDiscovery(ref _hidDevice);

            DeviceChangeNotifier a = new DeviceChangeNotifier(_hidDevice);
            DeviceChangeNotifier.Start();

            if (_hidDiscovery.FindTargetDevice())
            {
                //Open(DevicePath);
            }

            
        }
        #endregion

        #region Public Methods
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
            if (DataSent == null) return;
            var reportData = new byte[MaxOutputReportLength - 1];
            Array.Copy(report.Buffer, 1, reportData, 0, reportData.Length);
            DataSent(this, new DataSentEventArgs(reportData));
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
