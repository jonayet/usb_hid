using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UsbHid.USB.Classes;
using UsbHid.USB.Classes.DllWrappers;

namespace INFRA.USB
{
    internal class HidDiscovery : Win32Usb
    {
        private readonly HidDevice _hidDevice;

        public HidDiscovery(ref HidDevice hidDevice)
        {
            _hidDevice = hidDevice;
        }

        internal bool FindTargetDevice()
        {
            // first, build the path search string
            // if both VendorID & ProductID are zero, search by PathString
            // otherwise, search by  VendorID & ProductID
            string searchText;
            if (_hidDevice.VendorID == 0 && _hidDevice.ProductID == 0)
            {
                searchText = _hidDevice.PathString;
                if(string.IsNullOrEmpty(searchText)) { return false; }
            }
            else
            {
                searchText = string.Format("vid_{0:x4}&pid_{1:x4}", _hidDevice.VendorID, _hidDevice.ProductID);
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
                var devicePathList = new List<string>();

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

                // check device index
                if (devicePathList.Count > _hidDevice.Index)
                {
                    if (!string.IsNullOrEmpty(devicePathList[_hidDevice.Index]))
                    {
                        _hidDevice.PathString = devicePathList[_hidDevice.Index];
                        _hidDevice.IsAttached = true;
                    }
                }

                // is the device attached?
                if (_hidDevice.IsAttached)
                {
                    Debug.WriteLine("usbGenericHidCommunication:findTargetDevice() -> Performing CreateFile for HidHandle");
                    _hidDevice.HidHandle = Kernel32.CreateFile(
                        _hidDevice.PathString, 0,
                        Constants.FileShareRead | Constants.FileShareWrite,
                        IntPtr.Zero, Constants.OpenExisting,
                        0, 0);

                    // Did we open the ReadHandle successfully?
                    if (_hidDevice.HidHandle.IsInvalid)
                    {
                        throw new ApplicationException("usbGenericHidCommunication:findTargetDevice() -> Unable to open a HidHandle to the device!");
                    }

                    // Query the HID device's capabilities (primarily we are only really interested in the 
                    // input and output report byte lengths as this allows us to validate information sent
                    // to and from the device does not exceed the devices capabilities.
                    //
                    // We could determine the 'type' of HID device here too, but since this class is only
                    // for generic HID communication we don't care...
                    GetCapabilities();

                    // Open the readHandle to the device
                    Debug.WriteLine(string.Format("usbGenericHidCommunication:findTargetDevice() -> Opening a readHandle to the device"));
                    _hidDevice.ReadHandle = Kernel32.CreateFile(
                        _hidDevice.PathString,
                        Constants.GenericRead,
                        Constants.FileShareRead | Constants.FileShareWrite,
                        IntPtr.Zero, Constants.OpenExisting,
                        Constants.FileFlagOverlapped,
                        0);

                    // Did we open the ReadHandle successfully?
                    if (_hidDevice.ReadHandle.IsInvalid)
                    {
                        throw new ApplicationException("usbGenericHidCommunication:findTargetDevice() -> Unable to open a readHandle to the device!");
                    }

                    Debug.WriteLine(string.Format("usbGenericHidCommunication:findTargetDevice() -> Opening a writeHandle to the device"));
                    _hidDevice.WriteHandle = Kernel32.CreateFile(
                        _hidDevice.PathString,
                        Constants.GenericWrite,
                        Constants.FileShareRead | Constants.FileShareWrite,
                        IntPtr.Zero,
                        Constants.OpenExisting, 0, 0);

                    // Did we open the writeHandle successfully?
                    if (_hidDevice.WriteHandle.IsInvalid)
                    {
                        throw new ApplicationException("usbGenericHidCommunication:findTargetDevice() -> Unable to open a writeHandle to the device!");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                _hidDevice.ReadHandle.Close();
                _hidDevice.WriteHandle.Close();
                _hidDevice.HidHandle.Close();
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hInfoSet);
            }
            return false;
        }

        #region Private Methods
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
                //var size = IntPtr.Size == 4 ? (4 + Marshal.SystemDefaultCharSize) : 8;
                var oDetail = new DeviceInterfaceDetailData { Size = 5 }; // hardcoded to 5
                if (SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, ref oDetail, nRequiredSize, ref nRequiredSize, IntPtr.Zero))
                {
                    return oDetail.DevicePath;
                }
            }
            return string.Empty;
        }

        private bool GetCapabilities()
        {
            var preparsedData = new IntPtr();
            try
            {
                // Get the preparsed data from the HID driver
                if (!Hid.HidD_GetPreparsedData(_hidDevice.HidHandle, ref preparsedData))
                {
                    return false;
                }

                // extract the device capabilities from the internal buffer
                HidCaps oCaps;
                HidP_GetCaps(preparsedData, out oCaps);
                _hidDevice.MaxInputReportLength = oCaps.InputReportByteLength;
                _hidDevice.MaxOutputReportLength = oCaps.OutputReportByteLength;
                return true;
            }
            finally
            {
                // Free up the memory before finishing
                if (preparsedData != IntPtr.Zero)
                {
                    Hid.HidD_FreePreparsedData(preparsedData);
                }
            }
        }
        #endregion
    }
}
