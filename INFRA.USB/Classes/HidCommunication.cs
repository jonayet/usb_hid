using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using INFRA.USB.DllWrappers;

namespace INFRA.USB.Classes
{
	/// <summary>
	/// Abstract HID device : Derive your new device controller class from this
	/// </summary>
    public class HidCommunication : IDisposable
    {
        #region Public Fields
	    public event ReportRecievedEventHandler ReportReceived;
        #endregion

        #region private / internal Fields
	    private readonly HidDevice _hidDevice;
        private FileStream _usbReadFileStream;
        private FileStream _usbWriteFileStream;
	    #endregion

        #region Constructor
        public HidCommunication(ref HidDevice hidDevice)
	    {
            _hidDevice = hidDevice;
	    }
        #endregion

        #region Public Methods
        /// <summary>
		/// Open the device stream
		/// </summary>
        public bool Open()
        {
            if (!_hidDevice.IsAttached) { return false; }
            if (_hidDevice.IsOpen) { return false; }

            // do we have device PathString?
            if (string.IsNullOrEmpty(_hidDevice.PathString))
            {
                _hidDevice.PathString = HidDeviceDiscovery.GetPathString(_hidDevice.VendorID, _hidDevice.ProductID, _hidDevice.Index);
            }

            try
            {
                Debug.WriteLine("usbGenericHidCommunication:HidCommunication() -> Performing CreateFile for HidHandle");
                _hidDevice.HidHandle = Kernel32.CreateFile(
                    _hidDevice.PathString, 0,
                    Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE,
                    IntPtr.Zero, Constants.OPEN_EXISTING,
                    0, 0);

                // Did we open the ReadHandle successfully?
                if (_hidDevice.HidHandle.IsInvalid)
                {
                    throw new ApplicationException(
                        "usbGenericHidCommunication:HidCommunication() -> Unable to open a HidHandle to the device!");
                }


                // Query the HID device's capabilities (primarily we are only really interested in the 
                // input and output report byte lengths as this allows us to validate information sent
                // to and from the device does not exceed the devices capabilities.
                //
                // We could determine the 'type' of HID device here too, but since this class is only
                // for generic HID communication we don't care...
                GetCapabilities();
                GetAdditionalDeviceInfo();

                // Open the readHandle to the device
                Debug.WriteLine(string.Format("usbGenericHidCommunication:HidCommunication() -> Opening a readHandle to the device"));
                _hidDevice.ReadHandle = Kernel32.CreateFile(
                    _hidDevice.PathString,
                    Constants.GENERIC_READ,
                    Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE,
                    IntPtr.Zero, Constants.OPEN_EXISTING,
                    Constants.FILE_FLAG_OVERLAPPED,
                    0);

                // Did we open the ReadHandle successfully?
                if (_hidDevice.ReadHandle.IsInvalid)
                {
                    throw new ApplicationException(
                        "usbGenericHidCommunication:HidCommunication() -> Unable to open a readHandle to the device!");
                }

                Debug.WriteLine(
                    string.Format("usbGenericHidCommunication:HidCommunication() -> Opening a writeHandle to the device"));
                _hidDevice.WriteHandle = Kernel32.CreateFile(
                    _hidDevice.PathString,
                    Constants.GENERIC_WRITE,
                    Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    Constants.OPEN_EXISTING, 0, 0);

                // Did we open the writeHandle successfully?
                if (_hidDevice.WriteHandle.IsInvalid)
                {
                    throw new ApplicationException(
                        "usbGenericHidCommunication:HidCommunication() -> Unable to open a writeHandle to the device!");
                }
                Debug.WriteLine(string.Format("usbGenericHidCommunication:HidCommunication() -> Opening successful!---------------------- :)"));

                // start async reading
                _usbWriteFileStream = new FileStream(_hidDevice.WriteHandle, FileAccess.Write, _hidDevice.MaxOutputReportLength, false);
                _usbReadFileStream = new FileStream(_hidDevice.ReadHandle, FileAccess.Read, _hidDevice.MaxInputReportLength, true);
                BeginAsyncRead();
                
                _hidDevice.IsOpen = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                try
                {
                    if (_usbReadFileStream != null) { _usbReadFileStream.Close(); }
                    if (!_hidDevice.ReadHandle.IsInvalid) { _hidDevice.ReadHandle.Close(); }
                    if (!_hidDevice.WriteHandle.IsInvalid) { _hidDevice.WriteHandle.Close(); }
                    if (!_hidDevice.HidHandle.IsInvalid) { _hidDevice.HidHandle.Close(); }
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Close the device stream
        /// </summary>
        public void Close()
        {
            try
            {
                Debug.WriteLine(string.Format("usbGenericHidCommunication:HidCommunication() -> start closing handles..."));
                Monitor.PulseAll(_usbReadFileStream);
                if (_usbReadFileStream != null) { _usbReadFileStream.Close(); }
                if (!_hidDevice.ReadHandle.IsInvalid) { _hidDevice.ReadHandle.Close(); }
                if (!_hidDevice.WriteHandle.IsInvalid) { _hidDevice.WriteHandle.Close(); }
                if (!_hidDevice.HidHandle.IsInvalid) { _hidDevice.HidHandle.Close(); }
                _hidDevice.IsOpen = false;
                Debug.WriteLine(string.Format("usbGenericHidCommunication:HidCommunication() -> closing complete!----------------"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public bool WriteReport(HidOutputReport report)
        {
            // Make sure a device is attached & opened
            if (!_hidDevice.IsAttached | !_hidDevice.IsOpen)
            {
                Debug.WriteLine(string.Format("usbGenericHidCommunication:writeReportToDevice(): -> No device attached or Target device is not Opened!"));
                return false;
            }

            try
            {
                lock (_usbWriteFileStream)
                {
                    Debug.WriteLine(string.Format("usbGenericHidCommunication:WriteReport(): -> start Writing"));
                    _usbWriteFileStream.Write(report.ReportData, 0, report.ReportData.Length);
                }
                return true;
            }
            catch (IOException ex)
            {
                // An error - send out some debug and return failure
                Debug.WriteLine("usbGenericHidCommunication:WriteReport(): -> EXCEPTION: When attempting to send an output report");
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Kicks off an asynchronous read which completes when data is read or when the device
        /// is disconnected. Uses a callback.
        /// </summary>
        private void BeginAsyncRead()
        {
            var arrInputReport = new byte[_hidDevice.MaxInputReportLength];
            try
            {
                // put the buff we used to receive the stuff as the async state then we can get at it when the read completes
                lock (_usbReadFileStream)
                {
                    _usbReadFileStream.BeginRead(arrInputReport, 0, _hidDevice.MaxInputReportLength, AsyncReadCompleted, arrInputReport);
                    Monitor.Wait(_usbReadFileStream, 1000);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine("usbGenericHidCommunication:BeginAsyncRead(): -> EXCEPTION: When attempting to async read of an input report");
                Debug.WriteLine(ex.ToString());
                //throw new HidDeviceException("Device was removed.");
            }
        }

        /// <summary>
        /// Callback for above. Care with this as it will be called on the background thread from the async read
        /// </summary>
        /// <param name="iResult">Async result parameter</param>
        private void AsyncReadCompleted(IAsyncResult iResult)
        {
            // retrieve the read buffer
            var reportData = (byte[])iResult.AsyncState;
            try
            {
                // call end read : this throws any exceptions that happened during the read
                lock (_usbReadFileStream)
                {
                    _usbReadFileStream.EndRead(iResult);
                    Monitor.Pulse(_usbReadFileStream);
                }

                try
                {
                    if (ReportReceived != null)
                    {
                        ReportReceived(this, new ReportRecievedEventArgs(new HidInputReport{ReportData = reportData}));
                    }
                }
                finally
                {
                    // when all that is done, kick off another read for the next report
                    BeginAsyncRead();
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.ToString());
                //throw new HIDDeviceException("Device was removed.");
            }
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
                Structures.HidCaps oCaps;
                Hid.HidP_GetCaps(preparsedData, out oCaps);
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

        private void GetAdditionalDeviceInfo()
        {
            var attributes = new Structures.HidAttributes();
            var stringBuilder = new StringBuilder(256, 256);
            attributes.Size = Marshal.SizeOf(attributes);
            _hidDevice.ProductVersion = Hid.HidD_GetAttributes(_hidDevice.HidHandle, ref attributes) ? attributes.VersionNumber : 0;
            _hidDevice.Manufacturer = Hid.HidD_GetManufacturerString(_hidDevice.HidHandle, stringBuilder, 256) ? stringBuilder.ToString() : "";
            _hidDevice.ProductName = Hid.HidD_GetProductString(_hidDevice.HidHandle, stringBuilder, 256) ? stringBuilder.ToString() : "";
            _hidDevice.SerialNumber = Hid.HidD_GetSerialNumberString(_hidDevice.HidHandle, stringBuilder, 256) ? stringBuilder.ToString() : "";
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Disposer called by both dispose and finalise
        /// </summary>
        /// <param name="bDisposing">True if disposing</param>
        protected virtual void Dispose(bool bDisposing)
        {
            try
            {
                if (bDisposing)	// if we are disposing, need to close the managed resources
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        #endregion
    }   

    #region Custom exception
    /// <summary>
    /// Generic HID device exception
    /// </summary>
    public class HidDeviceException : ApplicationException
    {
        public HidDeviceException(string strMessage) : base(strMessage) { }

        public static HidDeviceException GenerateWithWinError(string strMessage)
        {
            return new HidDeviceException(string.Format("Msg:{0} WinEr:{1:X8}", strMessage, Marshal.GetLastWin32Error()));
        }

        public static HidDeviceException GenerateError(string strMessage)
        {
            return new HidDeviceException(string.Format("Msg:{0}", strMessage));
        }
    }
    #endregion
}
