using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using INFRA.USB.DllWrappers;

namespace INFRA.USB.Classes
{
    internal class HidDeviceDiscovery
    {
        private readonly HidDevice _hidDevice;

        public HidDeviceDiscovery(ref HidDevice hidDevice)
        {
            _hidDevice = hidDevice;
        }

        public bool FindTargetDevice()
        {
            // Initialise the internal variables required for performing the search
            Guid hidGuid;
            var systemHidGuid = Hid.HIDGuid;
            var devicePathList = new List<string>();

            // build the path search string
            // if both VendorID & ProductID are zero, search by PathString
            // otherwise, search by  VendorID & ProductID
            string searchText;
            if (_hidDevice.VendorID == 0 && _hidDevice.ProductID == 0)
            {
                if (string.IsNullOrEmpty(_hidDevice.PathString)) { return false; }
                searchText = _hidDevice.PathString.ToLower();
            }
            else
            {
                searchText = string.Format("vid_{0:x4}&pid_{1:x4}", _hidDevice.VendorID, _hidDevice.ProductID);
            }

            Hid.HidD_GetHidGuid(out hidGuid);
            var deviceInfoSet = SetupApi.SetupDiGetClassDevs(ref systemHidGuid, IntPtr.Zero, IntPtr.Zero, Constants.DIGCF_PRESENT | Constants.DIGCF_DEVICEINTERFACE);
            if (deviceInfoSet != Constants.InvalidHandle)
            {
                try
                {
                    var did = new Structures.SpDeviceInterfaceData();
                    did.cbSize = Marshal.SizeOf(did);

                    for (int i = 0; SetupApi.SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, i, ref did); i++)
                    {
                        var didetail = new Structures.SpDeviceInterfaceDetailData();
                        didetail.Size = (IntPtr.Size == 8) ? 8 : (4 + Marshal.SystemDefaultCharSize);
                        if (SetupApi.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref did, ref didetail, Marshal.SizeOf(didetail) - (int)Marshal.OffsetOf(didetail.GetType(), "DevicePath"), IntPtr.Zero, IntPtr.Zero))
                        {
                            //do a string search, if we find the VID/PID string then we found our device!
                            var devicePath = didetail.DevicePath;
                            if (devicePath != null && devicePath.ToLower().Contains(searchText))
                            {
                                devicePathList.Add(devicePath);
                            }   
                        }
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(string.Format("usbGenericHidCommunication:findHidDevices() -> EXCEPTION: Something went south whilst trying to get devices with matching GUIDs - giving up!"));
                    return false;
                }
                finally
                {
                    SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            // check back device index
            if (devicePathList.Count > _hidDevice.Index)
            {
                if (!string.IsNullOrEmpty(devicePathList[_hidDevice.Index]))
                {
                    _hidDevice.PathString = devicePathList[_hidDevice.Index];
                    _hidDevice.IsAttached = true;
                }
            }

            // is the device attached?
            if (!_hidDevice.IsAttached)
            {
                Debug.WriteLine("usbGenericHidCommunication:findTargetDevice() -> Target device not found! --------------------- :(");
                return false;
            }

            Debug.WriteLine("usbGenericHidCommunication:findTargetDevice() -> Target device found! -------------------------- :)");
            return true;
        }


        internal static string GetPathString(ushort vid, ushort pid, int index)
        {
            // Initialise the internal variables required for performing the search
            Guid hidGuid;
            var systemHidGuid = Hid.HIDGuid;
            var devicePathList = new List<string>();
            string searchText = string.Format("vid_{0:x4}&pid_{1:x4}", vid, pid);

            Hid.HidD_GetHidGuid(out hidGuid);
            var deviceInfoSet = SetupApi.SetupDiGetClassDevs(ref systemHidGuid, IntPtr.Zero, IntPtr.Zero, Constants.DIGCF_PRESENT | Constants.DIGCF_DEVICEINTERFACE);
            if (deviceInfoSet != Constants.InvalidHandle)
            {
                try
                {
                    var did = new Structures.SpDeviceInterfaceData();
                    did.cbSize = Marshal.SizeOf(did);

                    for (int i = 0; SetupApi.SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, i, ref did); i++)
                    {
                        var didetail = new Structures.SpDeviceInterfaceDetailData();
                        didetail.Size = (IntPtr.Size == 8) ? 8 : (4 + Marshal.SystemDefaultCharSize);
                        if (SetupApi.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref did, ref didetail, Marshal.SizeOf(didetail) - (int)Marshal.OffsetOf(didetail.GetType(), "DevicePath"), IntPtr.Zero, IntPtr.Zero))
                        {
                            //do a string search, if we find the VID/PID string then we found our device!
                            var devicePath = didetail.DevicePath;
                            if (devicePath != null && devicePath.ToLower().Contains(searchText))
                            {
                                devicePathList.Add(devicePath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("usbGenericHidCommunication:GetPathString() -> EXCEPTION: Something went south whilst trying to get devices with matching GUIDs - giving up!"));
                    return null;
                }
                finally
                {
                    SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            // check back device index
            if (devicePathList.Count > index)
            {
                if (!string.IsNullOrEmpty(devicePathList[index])) { return devicePathList[index]; }
            }
            return null;
        }
    }
}
