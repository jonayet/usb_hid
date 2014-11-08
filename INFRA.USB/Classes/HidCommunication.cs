using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using INFRA.USB.DllWrappers;

namespace INFRA.USB.Classes
{
	/// <summary>
	/// Abstract HID device : Derive your new device controller class from this
	/// </summary>
    internal class HidCommunication : Structures, IDisposable
    {
        #region private / internal Fields
	    private readonly HidDevice _hidDevice;

		/// <summary>Filestream we can use to read/write from</summary>
        private FileStream _usbReadFileStream;
        private FileStream _usbWriteFileStream;
	    #endregion

        #region constructor
        public HidCommunication(ref HidDevice hidDevice)
	    {
            _hidDevice = hidDevice;
	    }
        #endregion

        #region Protected Methods
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

                // Open the readHandle to the device
                Debug.WriteLine(
                    string.Format("usbGenericHidCommunication:HidCommunication() -> Opening a readHandle to the device"));
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
                _usbWriteFileStream = new FileStream(_hidDevice.WriteHandle, FileAccess.Write, _hidDevice.MaxOutputReportLength);
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

		/// <summary>
		/// Write an output report to the device.
		/// </summary>
        /// <param name="report">Output report to write</param>
        protected void Write(OutputReport report)
        {
            if (report == null || report.Buffer == null) { throw new HidDeviceException("Null data."); }
            if (report.Buffer.Length > _hidDevice.MaxOutputReportLength) { Array.Resize(ref report.Buffer, _hidDevice.MaxOutputReportLength); }

		    try
            {
                _usbWriteFileStream.Write(report.Buffer, 0, report.Buffer.Length);
                OnDataSent(report);
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.ToString());
                //throw new HidDeviceException("Device was removed.");
            }
        }
        #endregion

        #region Private Methods
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
                _usbReadFileStream.BeginRead(arrInputReport, 0, _hidDevice.MaxInputReportLength, ReadCompleted, arrInputReport);
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.ToString());
                //throw new HidDeviceException("Device was removed.");
            }
        }

        /// <summary>
        /// Callback for above. Care with this as it will be called on the background thread from the async read
        /// </summary>
        /// <param name="iResult">Async result parameter</param>
        private void ReadCompleted(IAsyncResult iResult)
        {
            // retrieve the read buffer
            var arrBuff = (byte[])iResult.AsyncState;
            try
            {
                // call end read : this throws any exceptions that happened during the read
                _usbReadFileStream.EndRead(iResult);

                OnDataReceived(new InputReport {Buffer = arrBuff});

                // when all that is done, kick off another read for the next report
                BeginAsyncRead();
            }
            catch (IOException ex)
            {
                Debug.WriteLine(ex.ToString());
                //throw new HIDDeviceException("Device was removed.");
            }
        }
        #endregion

        #region Protected Virtual Methods
        /// <summary>
        /// virtual handler for any action to be taken when data is sent. Override to use.
        /// </summary>
        protected virtual void OnDataSent(OutputReport report)
        {

        }

        /// <summary>
        /// virtual handler for any action to be taken when data is received. Override to use.
        /// </summary>
        /// <param name="report">The input report that was received</param>
        protected virtual void OnDataReceived(InputReport report)
        {

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
                    if (_usbReadFileStream != null)
                    {
                        _usbReadFileStream.Close();
                        _usbReadFileStream = null;
                    }
                    _hidDevice.ReadHandle.Close();
                    _hidDevice.WriteHandle.Close();
                    _hidDevice.HidHandle.Close();
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
