using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using INFRA.USB.DllWrappers;
using Microsoft.Win32.SafeHandles;

namespace INFRA.USB.HelperClasses
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

        #region Static Methods
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

        private static HidDevice CreateDeviceFromPath(string devicePath)
        {
            string productName;
            string manufacturer;
            string serialNumber;
            int productVersion;
            int inputReportLength;
            int outputReportLength;

            devicePath = devicePath.ToLower();
            var sVid = devicePath.Substring(devicePath.IndexOf("vid_", StringComparison.CurrentCulture) + 4, 4); ;
            var sPid = devicePath.Substring(devicePath.IndexOf("pid_", StringComparison.CurrentCulture) + 4, 4); ;
            ushort vid = ushort.Parse(sVid, System.Globalization.NumberStyles.HexNumber);
            ushort pid = ushort.Parse(sPid, System.Globalization.NumberStyles.HexNumber);

            var deviceHandle = new SafeFileHandle(IntPtr.Zero, false);
            try
            {
                var stringBuilder = new StringBuilder(256, 256);
                var attributes = new Structures.HidAttributes();
                attributes.Size = Marshal.SizeOf(attributes);
                deviceHandle = Kernel32.CreateFile(devicePath, 0, Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE, IntPtr.Zero, Constants.OPEN_EXISTING, 0, 0);
                productName = Hid.HidD_GetProductString(deviceHandle, stringBuilder, 256) ? stringBuilder.ToString() : "";
                manufacturer = Hid.HidD_GetManufacturerString(deviceHandle, stringBuilder, 256) ? stringBuilder.ToString() : "";
                serialNumber = Hid.HidD_GetSerialNumberString(deviceHandle, stringBuilder, 256) ? stringBuilder.ToString() : "";
                productVersion = Hid.HidD_GetAttributes(deviceHandle, ref attributes) ? attributes.VersionNumber : 0;
            }
            finally
            {
                if (!deviceHandle.IsInvalid)
                    deviceHandle.Close();
            }

            var preparsedData = new IntPtr();
            try
            {
                Structures.HidCaps oCaps;
                Hid.HidP_GetCaps(preparsedData, out oCaps);
                inputReportLength = oCaps.InputReportByteLength;
                outputReportLength = oCaps.OutputReportByteLength;
            }
            finally
            {
                // Free up the memory before finishing
                if (preparsedData != IntPtr.Zero)
                    Hid.HidD_FreePreparsedData(preparsedData);
            }

            var aHidDeviceDevice = new HidDevice
            {
                VendorID = vid,
                ProductID = pid,
                PathString = devicePath,
                ProductName = productName,
                Manufacturer = manufacturer,
                SerialNumber = serialNumber,
                ProductVersion = productVersion,
                MaxInputReportLength = inputReportLength,
                MaxOutputReportLength = outputReportLength,
                IsAttached = true,
                Index = 0,
            };
            return aHidDeviceDevice;
        }

        internal static List<HidDevice> GetDeviceList()
        {
            // Initialise the internal variables required for performing the search
            Guid hidGuid;
            var systemHidGuid = Hid.HIDGuid;
            var deviceList = new List<HidDevice>();

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
                            if (!string.IsNullOrEmpty(devicePath))
                            {
                                deviceList.Add(CreateDeviceFromPath(devicePath));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("usbGenericHidCommunication:findHidDevices() -> EXCEPTION: Something went south whilst trying to get devices with matching GUIDs - giving up!"));
                    return deviceList;
                }
                finally
                {
                    SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return deviceList;
        } 
        #endregion
    }
}
