using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using INFRA.USB.Classes;
using INFRA.USB.DllWrappers;

namespace INFRA.USB
{
    internal class HidModule
    {
        #region Public Fields
        /// <summary>Details Device Information</summary>
        public HidDevice HidDevice
        {
            get
            {
                //var stringBuilder = new StringBuilder();
                //var devInfo = new HidDevice()
                //{
                //    VendorID = _hidDevice.VendorID,
                //    ProductID = _hidDevice.ProductID,
                //    Index = _hidDevice.Index,
                //    PathString = _hidDevice.PathString,
                //    Manufacturer = Hid.HidD_GetManufacturerString(_hidDevice.HidHandle, stringBuilder, 256) ? stringBuilder.ToString() : "",
                //    ProductName = Hid.HidD_GetProductString(_hidDevice.HidHandle, stringBuilder, 256) ? stringBuilder.ToString() : "",
                //    SerialNumber = Hid.HidD_GetSerialNumberString(_hidDevice.HidHandle, stringBuilder, 256) ? stringBuilder.ToString() : "",
                //    MaxInputReportLength = _hidDevice.MaxInputReportLength,
                //    MaxOutputReportLength = _hidDevice.MaxOutputReportLength,
                //    ProductVersion = _productVersion.ToString(),
                //};
                //return devInfo;
                return _hidDevice;
            }
        }

        #endregion

        #region private fields
        private HidDevice _hidDevice;
        private HidDeviceDiscovery _hidDeviceDiscovery;
        public HidCommunication HIDCommunication;
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
            _hidDeviceDiscovery = new HidDeviceDiscovery(ref _hidDevice);
            HIDCommunication = new HidCommunication(ref _hidDevice);

            // start Hid device Notifier event
            var devNotifier = new HidDeviceNotifier(ref _hidDevice);
            devNotifier.Start();

            if (_hidDeviceDiscovery.FindTargetDevice())
            {
                if (DeviceAttached != null) DeviceAttached(this, EventArgs.Empty);
                HIDCommunication.Open();
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
            _hidDeviceDiscovery = new HidDeviceDiscovery(ref _hidDevice);
            HIDCommunication = new HidCommunication(ref _hidDevice);
            
            // start Hid device Notifier event
            var devNotifier = new HidDeviceNotifier(ref _hidDevice);
            HidDeviceNotifier.DeviceAttached += new EventHandler(devNotifier_DeviceAttached);
            HidDeviceNotifier.DeviceDetached += new EventHandler(devNotifier_DeviceDetached);
            devNotifier.Start();
        }

        void devNotifier_DeviceAttached(object sender, EventArgs e)
        {
            if (DeviceAttached != null) DeviceAttached(this, EventArgs.Empty);
            HIDCommunication.Open();
        }

        void devNotifier_DeviceDetached(object sender, EventArgs e)
        {
            HIDCommunication.Close();   
            if (DeviceDetached != null) DeviceDetached(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods
        internal void FindTargetDevice()
        {
            if (_hidDeviceDiscovery.FindTargetDevice())
            {
                if (DeviceAttached != null) DeviceAttached(this, EventArgs.Empty);
                HIDCommunication.Open();
            }
        }
        #endregion

        #region Private helper methods
        private bool GetAttributes()
        {
            var attributes = new Structures.HidDAttributes();
            attributes.Size = Marshal.SizeOf(attributes);
            if (!Hid.HidD_GetAttributes(_hidDevice.HidHandle, ref attributes)) { return false; }
            _productVersion = attributes.VersionNumber;
            return true;
        }
        #endregion

        #region Event properties
        /// <summary>
        /// Event handler called after Data sent complete.
        /// </summary>
        internal event EventHandler DeviceAttached;

        /// <summary>
        /// Event handler called when new data received.
        /// </summary>
        internal event EventHandler DeviceDetached;

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
        protected void OnDataSent(OutputReport report)
        {
            if (DataSent == null) return;
            var reportData = new byte[_hidDevice.MaxOutputReportLength - 1];
            Array.Copy(report.Buffer, 1, reportData, 0, reportData.Length);
            DataSent(this, new DataSentEventArgs(reportData));
        }

        protected void OnDataReceived(InputReport report)
        {
            // Fire the event handler if assigned
            if (DataReceived == null) return;
            var reportData = new byte[_hidDevice.MaxOutputReportLength - 1];
            Array.Copy(report.Buffer, 1, reportData, 0, reportData.Length);
            DataReceived(this, new DataRecievedEventArgs(reportData));
        } 
        #endregion

        #region Disposal methods
        protected void Dispose(bool bDisposing)
        {
            if (bDisposing)
            {
                // to do's before exit
            }
            //base.Dispose(bDisposing);
        } 
        #endregion
    }
}
