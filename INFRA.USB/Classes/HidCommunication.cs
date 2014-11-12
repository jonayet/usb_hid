using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
	    private const int InputReportBufferSize = 100;
        private const int NumberOfInputReportsForEvent = 100;
        #endregion

        #region private / internal Fields
	    private readonly HidDevice _hidDevice;
        private FileStream _usbReadFileStream;
        private FileStream _usbWriteFileStream;
	    private bool _forceSyncRead;
        private RingBuffer<HidInputReport> _inputReports;
	    #endregion

        #region constructor
        public HidCommunication(ref HidDevice hidDevice)
	    {
            _hidDevice = hidDevice;
            _inputReports = new RingBuffer<HidInputReport>(InputReportBufferSize);
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
                _usbWriteFileStream = new FileStream(_hidDevice.WriteHandle, FileAccess.Write, _hidDevice.MaxOutputReportLength, true);
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
                    _usbWriteFileStream.BeginWrite(report.ReportData, 0, report.ReportData.Length, RawReportWriteComplete, null);
                    Monitor.Wait(_usbWriteFileStream);
                }
                return true;
            }
            catch (IOException ex)
            {
                // An error - send out some debug and return failure
                Debug.WriteLine("usbGenericHidCommunication:writeReportToDevice(): -> EXCEPTION: When attempting to send an output report");
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        public bool ReadRawReportFromDevice(ref HidInputReport report)
        {
            // Make sure a device is attached & opened
            if (!_hidDevice.IsAttached | !_hidDevice.IsOpen)
            {
                Debug.WriteLine(string.Format("usbGenericHidCommunication:writeReportToDevice(): -> No device attached or Target device is not Opened!"));
                return false;
            }
            try
            {
                lock (_usbReadFileStream)
                {
                    _usbReadFileStream.BeginRead(report.ReportData, 0, _hidDevice.MaxInputReportLength, RawReportReadComplete, null);
                    Monitor.Wait(_usbReadFileStream);
                }
                return true;
            }
            catch (IOException ex)
            {
                // An error - send out some debug and return failure
                Debug.WriteLine(string.Format("usbGenericHidCommunication:readReportFromDevice(): -> EXCEPTION: When attempting to receive an input report"));
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }
        #endregion

	    public bool ReadSingleReportFromDevice(ref byte[] inputReportBuffer)
        {
            // The size of our inputReportBuffer must be at least the same size as the input report.
            if (inputReportBuffer.Length != _hidDevice.MaxInputReportLength)
            {
                // inputReportBuffer is not the right length!
                Debug.WriteLine(string.Format("usbGenericHidCommunication:readSingleReportFromDevice(): -> ERROR: The referenced inputReportBuffer size is incorrect for the input report size!"));
                return false;
            }

            // stop continuse read
	        _forceSyncRead = true;
            Monitor.Pulse(_usbReadFileStream);

            // The readRawReportFromDevice method will fill the passed readBuffer or return false
            return ReadRawReportFromDevice(ref inputReportBuffer);
        }

        public bool ReadMultipleReportsFromDevice(ref byte[] inputReportBuffer, int numberOfReports)
        {
            // Define a temporary buffer for assembling partial data reads into the completed inputReportBuffer
            var temporaryBuffer = new Byte[_hidDevice.MaxInputReportLength];
            long pointerToBuffer = 0;
            var success = false;

            // Range check the number of reports
            if (numberOfReports == 0)
            {
                Debug.WriteLine(string.Format("usbGenericHidCommunication:readMultipleReportsFromDevice(): -> ERROR: You cannot request 0 reports!"));
                return false;
            }

            if (numberOfReports > 128)
            {
                Debug.WriteLine(string.Format("usbGenericHidCommunication:readMultipleReportsFromDevice(): -> ERROR: Reference application testing does not verify the code for more than 128 reports"));
                return false;
            }

            // The size of our inputReportBuffer must be at least the same size as the input report multiplied by the number of reports requested.
            if (inputReportBuffer.Length != (_hidDevice.MaxInputReportLength * numberOfReports))
            {
                // inputReportBuffer is not the right length!
                Debug.WriteLine(string.Format("usbGenericHidCommunication:readMultipleReportsFromDevice(): -> ERROR: The referenced inputReportBuffer size is incorrect for the number of input reports requested!"));
                return false;
            }

            //Debug.WriteLine(string.Format("usbGenericHidCommunication:readMultipleReportsFromDevice(): -> Reading from device..."));
            // The readRawReportFromDevice method will fill the passed read buffer or return false
            while (pointerToBuffer != (_hidDevice.MaxInputReportLength * numberOfReports))
            {
                //Debug.WriteLine(string.Format("usbGenericHidCommunication:readMultipleReportsFromDevice(): -> Reading from device..."));
                success = ReadRawReportFromDevice(ref temporaryBuffer);

                // Was the read successful?
                if (!success) { return false; }

                // Copy the received data into the referenced input buffer
                Array.Copy(temporaryBuffer, 0, inputReportBuffer, pointerToBuffer, temporaryBuffer.Length);
                pointerToBuffer += temporaryBuffer.Length;
            }
            //Debug.WriteLine(string.Format("usbGenericHidCommunication:readMultipleReportsFromDevice(): -> Reading complete..."));
            return success;
        }

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

        private void RawReportReadComplete(IAsyncResult iResult)
        {
            lock (_usbReadFileStream)
            {
                //Debug.WriteLine(string.Format("usbGenericHidCommunication:readReportFromDevice(): -> Read Ok"));
                _usbReadFileStream.EndRead(iResult);
                Monitor.Pulse(_usbReadFileStream);
            }
        }

        private void RawReportWriteComplete(IAsyncResult iResult)
        {
            lock (_usbWriteFileStream)
            {
                //Debug.WriteLine(string.Format("usbGenericHidCommunication:readReportFromDevice(): -> Write complete"));
                _usbWriteFileStream.EndWrite(iResult);
                Monitor.Pulse(_usbWriteFileStream);
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
                lock (_usbReadFileStream)
                {
                    if (!_forceSyncRead)
                    {
                        _usbReadFileStream.BeginRead(arrInputReport, 0, _hidDevice.MaxInputReportLength, AsyncReadCompleted, arrInputReport);
                        Monitor.Wait(_usbReadFileStream);
                    }
                }
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
                    _inputReports.PutOverwriting(new HidInputReport {ReportData = reportData});
                }

                try
                {
                    //OnDataReceived(new InputReport {Buffer = arrBuff});
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
        #endregion

        #region Protected Virtual Methods
        /// <summary>
        /// virtual handler for any action to be taken when data is sent. Override to use.
        /// </summary>
        protected virtual void OnDataSent(HidOutputReport report)
        {

        }

        /// <summary>
        /// virtual handler for any action to be taken when data is received. Override to use.
        /// </summary>
        /// <param name="report">The input report that was received</param>
        protected virtual void OnDataReceived(HidInputReport report)
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
