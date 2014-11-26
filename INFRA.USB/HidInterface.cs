using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using INFRA.USB.HelperClasses;

namespace INFRA.USB
{
    /// <summary>
    /// This class provides an usb component. This can be placed ont to your form.
    /// </summary>
    [ToolboxBitmap(typeof(HidInterface), "UsbInterface.bmp")]
    public class HidInterface : Component
    {
        #region Public Fields
        [Description("The vendor id from the USB device you want to use")]
        [DefaultValue("(none)")]
        [Category("Embedded Details")]
        public ushort VendorID
        {
            get { return _hidDevice.VendorID; }
            set { _hidDevice.VendorID = value; }
        }

        [Description("The product id from the USB device you want to use")]
        [DefaultValue("(none)")]
        [Category("Embedded Details")]
        public ushort ProductID
        {
            get { return _hidDevice.ProductID; }
            set { _hidDevice.ProductID = value; }
        }

        [Description("The device index from the USB device you want to use")]
        [DefaultValue(0)]
        [Category("Embedded Details")]
        public int DeviceIndex
        {
            get { return _hidDevice.Index; }
            set { _hidDevice.Index = value; }
        }

        public static string NTString(char[] buffer)
        {
            int index = Array.IndexOf(buffer, '\0');
            return new string(buffer, 0, index >= 0 ? index : buffer.Length);
        }

        /// <summary>Details Device Information</summary>
        public HidDevice HidDevice
        {
            get
            {
                return _hidDevice;
            }
        }

        public string DevicePath
        {
            get { return _hidDevice.PathString; }
        }

        public bool IsAttached
        {
            get { return _hidDevice.IsAttached; }
        }

        #endregion

        #region private fields
        private readonly HidDevice _hidDevice;
        private readonly HidDeviceDiscovery _hidDeviceDiscovery;
        private readonly HidCommunication _hidCommunication;
        #endregion

        #region Constructors
        /// <summary>
        /// Initialize device with given path string.
        /// </summary>
        /// <param name="devicePath"></param>
        public HidInterface(string devicePath)
        {            
            _hidDevice = new HidDevice {PathString = devicePath};
            _hidCommunication = new HidCommunication(ref _hidDevice);
            _hidCommunication.ReportReceived += _hidCommunication_ReportReceived;
            _hidDeviceDiscovery = new HidDeviceDiscovery(ref _hidDevice);

            // start Hid device Notifier event
            var hidNotifier = new HidDeviceNotifier(ref _hidDevice);
            HidDeviceNotifier.DeviceAttached += new EventHandler(devNotifier_DeviceAttached);
            HidDeviceNotifier.DeviceDetached += new EventHandler(devNotifier_DeviceDetached);
            hidNotifier.Start();
        }

        /// <summary>
        /// Finds a device given its PID and VID
        /// </summary>
        /// <param name="vendorId">Vendor id for device (VID)</param>
        /// <param name="productId">Product id for device (PID)</param>
        /// <param name="index">Adress index if more than one device found.</param>
        public HidInterface(ushort vendorId, ushort productId, int index = 0)
        {
            _hidDevice = new HidDevice {VendorID = vendorId, ProductID = productId, Index = index};
            _hidCommunication = new HidCommunication(ref _hidDevice);
            _hidCommunication.ReportReceived += _hidCommunication_ReportReceived;
            _hidDeviceDiscovery = new HidDeviceDiscovery(ref _hidDevice);
            
            // start Hid device Notifier event
            var hidNotifier = new HidDeviceNotifier(ref _hidDevice);
            HidDeviceNotifier.DeviceAttached += new EventHandler(devNotifier_DeviceAttached);
            HidDeviceNotifier.DeviceDetached += new EventHandler(devNotifier_DeviceDetached);
            hidNotifier.Start();
        }
        #endregion

        #region Public Methods
        public void ConnectTargetDevice()
        {
            if (_hidDeviceDiscovery.FindTargetDevice())
            {
                if (OnDeviceAttached != null) OnDeviceAttached(this, EventArgs.Empty);
                _hidCommunication.Open();
            }
        }

        public bool Write(HidOutputReport report)
        {
            return _hidCommunication.WriteReport(report);
        }

        public bool Write(byte[] data)
        {
            return Write(new HidOutputReport {UserData = data});
        }
        #endregion

        #region Private helper methods
        private void devNotifier_DeviceAttached(object sender, EventArgs e)
        {
            if (OnDeviceAttached != null) OnDeviceAttached(this, e);
            _hidCommunication.Open();
        }

        private void devNotifier_DeviceDetached(object sender, EventArgs e)
        {
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

        /// <summary>
        /// Event handler called when new Serial received.
        /// </summary>
        private event SerialPacketRecievedEventHandler SerialPacketRecieved; 
        #endregion

        #region Overriden Methods
        #endregion
    }
}
