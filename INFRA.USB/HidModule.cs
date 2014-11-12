using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using INFRA.USB.Classes;
using INFRA.USB.DllWrappers;

namespace INFRA.USB
{
    /// <summary>
    /// This class provides an usb component. This can be placed ont to your form.
    /// </summary>
    [ToolboxBitmap(typeof(HidModule), "UsbHidBmp.bmp")]
    public class HidModule : Component
    {
        #region Public Fields
        [Description("The vendor id from the USB device you want to use")]
        [DefaultValue("(none)")]
        [Category("Embedded Details")]
        public ushort VendorID
        {
            get { return _vendorId; }
            set { _vendorId = value; }
        }

        [Description("The product id from the USB device you want to use")]
        [DefaultValue("(none)")]
        [Category("Embedded Details")]
        public ushort ProductID
        {
            get { return _productId; }
            set { _productId = value; }
        }

        [Description("The device index from the USB device you want to use")]
        [DefaultValue(0)]
        [Category("Embedded Details")]
        public int DeviceIndex
        {
            get { return _deviceIndex; }
            set { _deviceIndex = value; }
        }

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

        public string DevicePath
        {
            get { return _devicePath; }
        }

        public bool IsAttached
        {
            get { return _hidDevice.IsAttached; }
        }

        #endregion

        #region private fields
        private readonly HidDevice _hidDevice;
        private readonly HidDeviceDiscovery _hidDeviceDiscovery;
        private readonly HidCommunication HidCommunication;
        private int _productVersion;
        private ushort _vendorId;
        private ushort _productId;
        private int _deviceIndex;
        private string _devicePath;
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
            HidCommunication = new HidCommunication(ref _hidDevice);

            // start Hid device Notifier event
            HidDeviceNotifier.DeviceAttached += new EventHandler(devNotifier_DeviceAttached);
            HidDeviceNotifier.DeviceDetached += new EventHandler(devNotifier_DeviceDetached);
            var devNotifier = new HidDeviceNotifier(ref _hidDevice);
            devNotifier.Start();

            if (_hidDeviceDiscovery.FindTargetDevice())
            {
                if (DeviceAttached != null) DeviceAttached(this, EventArgs.Empty);
                HidCommunication.Open();
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
            HidCommunication = new HidCommunication(ref _hidDevice);
            
            // start Hid device Notifier event
            HidDeviceNotifier.DeviceAttached += new EventHandler(devNotifier_DeviceAttached);
            HidDeviceNotifier.DeviceDetached += new EventHandler(devNotifier_DeviceDetached);
            var devNotifier = new HidDeviceNotifier(ref _hidDevice);
            devNotifier.Start();
        }
        #endregion

        #region Public Methods
        public void FindTargetDevice()
        {
            if (_hidDeviceDiscovery.FindTargetDevice())
            {
                if (DeviceAttached != null) DeviceAttached(this, EventArgs.Empty);
                HidCommunication.Open();
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

        private void devNotifier_DeviceAttached(object sender, EventArgs e)
        {
            if (DeviceAttached != null) DeviceAttached(this, EventArgs.Empty);
            HidCommunication.Open();
        }

        private void devNotifier_DeviceDetached(object sender, EventArgs e)
        {
            HidCommunication.Close();
            if (DeviceDetached != null) DeviceDetached(this, EventArgs.Empty);
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
        internal event EventHandler DataSent;

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
        #endregion
    }
}
