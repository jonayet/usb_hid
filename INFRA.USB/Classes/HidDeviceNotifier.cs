﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using INFRA.USB.DllWrappers;

namespace INFRA.USB.Classes
{
    internal class HidDeviceNotifier : Form
    {
        #region event handlers
        public static event EventHandler DeviceAttached;
        public static event EventHandler DeviceDetached; 
        #endregion

        #region private fields
        private HidDevice _hidDevice;
        private HidDeviceNotifier _instance; 
        #endregion

        #region constructor
        public HidDeviceNotifier(ref HidDevice hidDevice)
        {
            _hidDevice = hidDevice;
        } 
        #endregion

        #region public methods
        public void Start()
        {
            var t = new Thread(RunForm);
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        public void Stop()
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
        #endregion

        #region private helper methods
        /// <summary>
        /// registerForDeviceNotification - registers the window (identified by the windowHandle) for 
        /// device notification messages from Windows
        /// </summary>
        private void RegisterForDeviceNotifications(IntPtr windowHandle)
        {
            Debug.WriteLine(string.Format("HidDeviceNotifier:RegisterForDeviceNotifications()"));
            User32.RegisterForUsbEvents(windowHandle, Hid.HIDGuid);
        }

        /// <summary>
        /// Handle System Device Notification for our Target Device
        /// </summary>
        /// <param name="m"></param>
        private void HandleDeviceNotificationMessages(Message m)
        {
            Debug.WriteLine(string.Format("HidDeviceNotifier:handleDeviceNotificationMessages() -> Method called"));

            // Make sure this is a device notification
            if (m.Msg != Constants.WM_DEVICECHANGE) { return; }
            Debug.WriteLine(string.Format("HidDeviceNotifier:handleDeviceNotificationMessages() -> Device notification received"));

            try
            {
                switch (m.WParam.ToInt32())
                {
                    // Device attached
                    case Constants.DEVICE_ARRIVAL:
                        Debug.WriteLine(string.Format("HidDeviceNotifier:handleDeviceNotificationMessages() -> A new device attached"));

                        // Was this our target device?  
                        if (IsNotificationForTargetDevice(m) && !_hidDevice.IsAttached)
                        {
                            _hidDevice.IsAttached = true;
                            // If so attach the USB device.
                            Debug.WriteLine(string.Format("HidDeviceNotifier:handleDeviceNotificationMessages() -> The target USB device has been attached -------- :)"));
                            ReportDeviceAttached(m);
                        }
                        break;

                    // Device removed
                    case Constants.DEVICE_REMOVECOMPLETE:
                        Debug.WriteLine(string.Format("HidDeviceNotifier:handleDeviceNotificationMessages() -> A device has been removed"));

                        // Was this our target device?  
                        if (IsNotificationForTargetDevice(m) && _hidDevice.IsAttached)
                        {
                            _hidDevice.IsAttached = false;
                            // If so detach the USB device.
                            Debug.WriteLine(string.Format("HidDeviceNotifier:handleDeviceNotificationMessages() -> The target USB device has been removed ----------- :("));
                            ReportDeviceDetached(m);
                        }
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine("HidDeviceNotifier:handleDeviceNotificationMessages() -> " + ex.ToString());
            }
        }
        
        private bool IsNotificationForTargetDevice(Message m)
        {
            try
            {
                var devBroadcastHeader = new Structures.DevBroadcastHdr();
                Marshal.PtrToStructure(m.LParam, devBroadcastHeader);

                // Is the notification event concerning a device interface?
                if (devBroadcastHeader.dbch_devicetype == Constants.DbtDevtypDeviceinterface)
                {
                    // Get the device path name of the affected device
                    var stringSize = Convert.ToInt32((devBroadcastHeader.dbch_size - 32) / 2);
                    var devBroadcastDeviceInterface = new Structures.DevBroadcastDeviceinterface
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
                    if (devicePathString.ToLower().Contains(searchText.ToLower())) { return true; }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("HidDeviceNotifier:isNotificationForTargetDevice() -> EXCEPTION: An unknown exception has occured!"));
                Debug.WriteLine(ex.Message);
            }
            return false;
        }

        private void RunForm()
        {
            Application.Run(new HidDeviceNotifier(ref _hidDevice));
        }

        private void EndForm()
        {
            Close();
        }

        private void ReportDeviceAttached(Message m)
        {
            if (DeviceAttached != null) DeviceAttached(this, EventArgs.Empty);
        } 

        private void ReportDeviceDetached(Message m)
        {
            if (DeviceDetached != null) DeviceDetached(this, EventArgs.Empty);
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
