using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using UsbHid.USB.Classes;

namespace INFRA.USB
{
    internal class DeviceChangeNotifier : Form
    {

        #region event handlers
        public delegate void DeviceAttachedDelegate();
        public static event DeviceAttachedDelegate DeviceAttached;

        public delegate void DeviceDetachedDelegate();
        public static event DeviceDetachedDelegate DeviceDetached; 
        #endregion

        #region private fields
        private HidDevice _hidDevice;
        private DeviceChangeNotifier _instance; 
        #endregion

        #region constructor
        public DeviceChangeNotifier()
        {

        }

        public DeviceChangeNotifier(ref HidDevice hidDevice)
        {
            _hidDevice = hidDevice;
        } 
        #endregion

        #region internal methods
        internal void Start()
        {
            var t = new Thread(RunForm);
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        internal void Stop()
        {
            try
            {
                if (_instance == null) throw new InvalidOperationException("Notifier not started");
                _instance.Invoke(new MethodInvoker(_instance.EndForm));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// registerForDeviceNotification - registers the window (identified by the windowHandle) for 
        /// device notification messages from Windows
        /// </summary>
        internal void RegisterForDeviceNotifications(IntPtr windowHandle)
        {
            Debug.WriteLine(string.Format("usbGenericHidCommunication:registerForDeviceNotifications() -> Method called"));
            Win32Usb.RegisterForUsbEvents(windowHandle, Win32Usb.HIDGuid);
        }

        /// <summary>
        /// Handle System Device Notification for our Target Device
        /// </summary>
        /// <param name="m"></param>
        internal void HandleDeviceNotificationMessages(Message m)
        {
            Debug.WriteLine(string.Format("usbGenericHidCommunication:handleDeviceNotificationMessages() -> Method called"));

            // Make sure this is a device notification
            if (m.Msg != Constants.WmDevicechange) { return; }
            Debug.WriteLine(string.Format("usbGenericHidCommunication:handleDeviceNotificationMessages() -> Device notification received"));

            try
            {
                switch (m.WParam.ToInt32())
                {
                    // Device attached
                    case Constants.DbtDevicearrival:
                        Debug.WriteLine(string.Format("usbGenericHidCommunication:handleDeviceNotificationMessages() -> A new device attached"));

                        // Was this our target device?  
                        if (IsNotificationForTargetDevice(m) && !_hidDevice.IsAttached)
                        {
                            _hidDevice.IsAttached = true;
                            // If so attach the USB device.
                            Debug.WriteLine(string.Format("usbGenericHidCommunication:handleDeviceNotificationMessages() -> The target USB device has been attached - opening..."));
                            ReportDeviceAttached(m);
                        }
                        break;

                    // Device removed
                    case Constants.DbtDeviceremovecomplete:
                        Debug.WriteLine(string.Format("usbGenericHidCommunication:handleDeviceNotificationMessages() -> A device has been removed"));

                        // Was this our target device?  
                        if (IsNotificationForTargetDevice(m) && _hidDevice.IsAttached)
                        {
                            _hidDevice.IsAttached = false;
                            // If so detach the USB device.
                            Debug.WriteLine(string.Format("usbGenericHidCommunication:handleDeviceNotificationMessages() -> The target USB device has been removed - closing..."));
                            ReportDeviceDetached(m);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("usbGenericHidCommunication:handleDeviceNotificationMessages() -> EXCEPTION: An unknown exception has occured!"));
                Debug.WriteLine(ex.Message);
            }
        } 
        #endregion

        #region private helper methods
        private bool IsNotificationForTargetDevice(Message m)
        {
            try
            {
                var devBroadcastHeader = new Win32Usb.DevBroadcastHdr();
                Marshal.PtrToStructure(m.LParam, devBroadcastHeader);

                // Is the notification event concerning a device interface?
                if (devBroadcastHeader.dbch_devicetype == Constants.DbtDevtypDeviceinterface)
                {
                    // Get the device path name of the affected device
                    var stringSize = Convert.ToInt32((devBroadcastHeader.dbch_size - 32) / 2);
                    var devBroadcastDeviceInterface = new Win32Usb.DevBroadcastDeviceinterface
                    {
                        dbcc_name = new Char[stringSize + 1]
                    };
                    Marshal.PtrToStructure(m.LParam, devBroadcastDeviceInterface);
                    var devicePathString = new string(devBroadcastDeviceInterface.dbcc_name, 0, stringSize);

                    // build the search string
                    // if both VendorID & ProductID  are zero, search by PathString
                    // otherwise, search by  VendorID & ProductID
                    string searchText;
                    if (_hidDevice.VendorID == 0 && _hidDevice.ProductID == 0)
                    {
                        searchText = _hidDevice.PathString;
                        if (string.IsNullOrEmpty(searchText)) { return false; }
                    }
                    else
                    {
                        searchText = string.Format("vid_{0:x4}&pid_{1:x4}", _hidDevice.VendorID, _hidDevice.ProductID);
                    }

                    // Compare the device name with our target device's VID, PID, Index or PathString (strings are moved to lower case)
                    return (devicePathString.ToLower().Contains(searchText.ToLower()));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("usbGenericHidCommunication:isNotificationForTargetDevice() -> EXCEPTION: An unknown exception has occured!"));
                Debug.WriteLine(ex.Message);
            }
            return false;
        }

        private void RunForm()
        {
            Application.Run(new DeviceChangeNotifier(ref _hidDevice));
        }

        private void EndForm()
        {
            Close();
        }

        private void ReportDeviceDetached(Message m)
        {
            if (DeviceDetached != null) DeviceDetached();
        }

        private void ReportDeviceAttached(Message m)
        {
            if (DeviceAttached != null) DeviceAttached();
        } 
        #endregion

        #region overriden methods
        protected override void SetVisibleCore(bool value)
        {
            // Prevent window getting visible
            if (_instance == null)
            {
                _instance = this;
                try
                {
                    CreateHandle();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            base.SetVisibleCore(false);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            RegisterForDeviceNotifications(Handle);
            base.OnHandleCreated(e);
        }

        protected override void WndProc(ref Message m)
        {
            HandleDeviceNotificationMessages(m);
            base.WndProc(ref m);
        } 
        #endregion
    }
}
