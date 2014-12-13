using System;
using System.ComponentModel;
using System.Drawing;
using INFRA.USB.HidHelper;
using Microsoft.Win32.SafeHandles;

// ReSharper disable CSharpWarnings::CS1591
// ReSharper disable InconsistentNaming

namespace INFRA.USB
{
    /// <summary>
    /// This class provides an usb component. This can be placed ont to our form.
    /// </summary>
    [ToolboxBitmap(typeof(HidDevice), "HidDevice.bmp")]
    public class HidDevice : Component
    {
        #region Public Properties
        [Description("The vendor id from the USB device we want to use")]
        [DefaultValue("(none)")]
        [Category("Embedded Details")]
        public ushort VendorID { get; internal set; }

        [Description("The product id from the USB device we want to use")]
        [DefaultValue("(none)")]
        [Category("Embedded Details")]
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
        public int ProductVersion { get; internal set; }

        /// <summary>
        /// Manufacturer
        /// </summary>
        public string Manufacturer { get; internal set; }

        /// <summary>
        /// Device attached flag
        /// </summary>
        public bool IsAttached { get; internal set; }

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
        /// Device handle open flag
        /// </summary>
        public bool IsConnected { get; internal set; } 
        #endregion

        #region private fields
        private HidCommunication _hidCommunication;
        private HidDeviceDiscovery _hidDeviceDiscovery;
        #endregion

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
            ProductVersion = 0;
            Manufacturer = "";
            IsAttached = false;
            HidHandle = null;
            ReadHandle = null;
            WriteHandle = null;
            IsConnected = false;
        }

        public HidDevice(ushort vendorId, ushort productId, int index = 0)
        {
            VendorID = vendorId;
            ProductID = productId;
            Index = index;
        }

        public void Connect()
        {
            string pathString = "";
            _hidDeviceDiscovery = new HidDeviceDiscovery();
            IsAttached = _hidDeviceDiscovery.FindDevice(VendorID, ProductID, Index, ref pathString);
            PathString = pathString;

            // create hid device from pathstring
            HidDevice hidDevice = new HidDevice
            {
                PathString = PathString,
                IsAttached = IsAttached,
            };

            // create hid communication from device
            _hidCommunication = new HidCommunication(ref hidDevice);
            _hidCommunication.ReportReceived += _hidCommunication_ReportReceived;

            if (IsAttached)
            {
                if (OnDeviceAttached != null) OnDeviceAttached(this, EventArgs.Empty);

                // open our device
                _hidCommunication.Open();

                // update current instance
                MaxInputReportLength = hidDevice.MaxInputReportLength;
                MaxOutputReportLength = hidDevice.MaxOutputReportLength;
                ProductName = hidDevice.ProductName;
                SerialNumber = hidDevice.SerialNumber;
                ProductVersion = hidDevice.ProductVersion;
                Manufacturer = hidDevice.Manufacturer;
                HidHandle = hidDevice.HidHandle;
                ReadHandle = hidDevice.ReadHandle;
                WriteHandle = hidDevice.WriteHandle;
                IsConnected = hidDevice.IsConnected;
            }

            // start Hid device Notifier event
            var hidNotifier = new HidDeviceNotifier(VendorID, ProductID, IsAttached);
            HidDeviceNotifier.DeviceAttached += devNotifier_DeviceAttached;
            HidDeviceNotifier.DeviceDetached += devNotifier_DeviceDetached;
            hidNotifier.Start();
        }

        public bool Write(HidOutputReport report)
        {
            return _hidCommunication.WriteReport(report);
        }

        public bool Write(byte[] data)
        {
            return Write(new HidOutputReport { UserData = data });
        }

        #region Private helper methods
        private void devNotifier_DeviceAttached(object sender, EventArgs e)
        {
            IsAttached = true;
            if (OnDeviceAttached != null) OnDeviceAttached(this, e);
            _hidCommunication.Open();
        }

        private void devNotifier_DeviceDetached(object sender, EventArgs e)
        {
            IsAttached = false;
            _hidCommunication.Close();
            if (OnDeviceRemoved != null) OnDeviceRemoved(this, e);
        }

        private void _hidCommunication_ReportReceived(object sender, ReportRecievedEventArgs e)
        {
            if (OnReportReceived != null) OnReportReceived(this, e);
        }
        #endregion

        #region Event properties
        /// <summary>
        /// Event handler called after Data sent complete.
        /// </summary>
        public event EventHandler OnDeviceAttached;

        /// <summary>
        /// Event handler called when new data received.
        /// </summary>
        public event EventHandler OnDeviceRemoved;

        /// <summary>
        /// Event handler called when new data received.
        /// </summary>
        public event ReportRecievedEventHandler OnReportReceived;
        #endregion
    }
}
