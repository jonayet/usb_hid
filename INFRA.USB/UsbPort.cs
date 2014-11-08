using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using INFRA.USB.Classes;
using INFRA.USB.DllWrappers;

namespace INFRA.USB
{
    /// <summary>
    /// This class provides an usb component. This can be placed ont to your form.
    /// </summary>
    [ToolboxBitmap(typeof (UsbPort), "UsbHidBmp.bmp")]
    public partial class UsbPort : Control
    {

        #region Private members
        private ushort _vendorId;
        private ushort _productId;
        private int _deviceIndex;
        private string _devicePath;
        private HidDevice _hidDevice;
        private IntPtr _usbEventHandle;
        private HidModule _hidModule;
        #endregion

        #region Events
        /// <summary>
        /// This event will be triggered when the device you specified is pluged into your usb port on
        /// the computer. And it is completly enumerated by windows and ready for use.
        /// </summary>
        [Description("The event that occurs when a usb hid device with the specified vendor id and product id is found on the bus")]
        [Category("Embedded Event")]
        [DisplayName("OnDeviceAttached")]
        public event EventHandler OnDeviceAttached;

        /// <summary>
        /// This event will be triggered when the device you specified is removed from your computer.
        /// </summary>
        [Description("The event that occurs when a usb hid device with the specified vendor id and product id is removed from the bus")]
        [Category("Embedded Event")]
        [DisplayName("OnDeviceRemoved")]
        public event EventHandler OnDeviceRemoved;

        /// <summary>
        /// This event will be triggered when data is recieved from the device specified by you.
        /// </summary>
        [Description("The event that occurs when data is recieved from the embedded system")]
        [Category("Embedded Event")]
        [DisplayName("OnDataRecieved")]
        public event DataRecievedEventHandler OnDataRecieved;

        /// <summary>
        /// This event will be triggered when data is send to the device. 
        /// It will only occure when this action wass succesfull.
        /// </summary>
        [Description("The event that occurs when data is send from the host to the embedded system")]
        [Category("Embedded Event")]
        [DisplayName("OnDataSent")]
        public event DataSentEventHandler OnDataSent; 
        #endregion

        #region Public Property
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

        public string DevicePath
        {
            get { return _devicePath; }
        }

        public bool IsConnected
        {
            get
            {
                return true; /*_hidCommunication.IsConnected;*/ }
        }

        public HidDevice HidDevice
        {
            get { return _hidDevice; }
        } 
        #endregion

        #region Constructor
        public UsbPort()
        {
            //initializing in initial state
            _vendorId = 0;
            _productId = 0;
            _deviceIndex = 0;
            _devicePath = "";
            _hidDevice = new HidDevice();

            InitializeComponent();
        }

        public UsbPort(ushort VID, ushort PID, int DeviceIndex = 0) : this()
        {
            //initializing in initial state
            _hidDevice.VendorID = VID;
            _hidDevice.ProductID = PID;
            _hidDevice.Index = DeviceIndex;

            _hidModule = new HidModule(VID, PID);
            _hidModule.DeviceAttached += new EventHandler(_hidModule_DeviceAttached);
            _hidModule.DeviceDetached += new EventHandler(_hidModule_DeviceDetached);
        }

        void _hidModule_DeviceAttached(object sender, EventArgs e)
        {
            if (OnDeviceAttached != null) OnDeviceAttached(this, EventArgs.Empty);
        } 

        void _hidModule_DeviceDetached(object sender, EventArgs e)
        {
            if (OnDeviceRemoved != null) OnDeviceRemoved(this, EventArgs.Empty);
        }
        #endregion

        /// <summary>
        /// Checks the devices that are present at the moment and checks if one of those
        /// is the device you defined by filling in the product id and vendor id.
        /// </summary>
        public void FindTargetDevice()
        {
            _hidModule.FindTargetDevice();
        }
    }
}
