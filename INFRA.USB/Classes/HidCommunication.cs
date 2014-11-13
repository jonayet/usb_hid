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
                Debug.WriteLine("HidCommunication:Open() -> Performing CreateFile for HidHandle");
                _hidDevice.HidHandle = Kernel32.CreateFile(
                    _hidDevice.PathString, 0,
                    Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE,
                    IntPtr.Zero, Constants.OPEN_EXISTING,
                    0, 0);

                // Did we open the ReadHandle successfully?
                if (_hidDevice.HidHandle.IsInvalid)
                {
                    throw new ApplicationException("HidCommunication:Open() -> Unable to open HidHandle to the device!");
                }

                // get MaxInputReportLength & MaxOutputReportLength and update the _hidDevice
                GetCapabilities();

                // get ProductVersion, Manufacturer, ProductName & SerialNumber and update the _hidDevice
                GetAdditionalDeviceInfo();

                // set report data length
                HidInputReport.ReportDataLength = _hidDevice.MaxInputReportLength;
                HidOutputReport.ReportDataLength = _hidDevice.MaxOutputReportLength;

                // Open the readHandle to the device
                Debug.WriteLine("HidCommunication:Open() -> Opening a ReadHandle to the device");
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
                    throw new ApplicationException("HidCommunication:Open() -> Unable to open a ReadHandle to the device!");
                }

                Debug.WriteLine("HidCommunication:Open() -> Opening a WriteHandle to the device");
                _hidDevice.WriteHandle = Kernel32.CreateFile(
                    _hidDevice.PathString,
                    Constants.GENERIC_WRITE,
                    Constants.FILE_SHARE_READ | Constants.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    Constants.OPEN_EXISTING, 0, 0);

                // Did we open the writeHandle successfully?
                if (_hidDevice.WriteHandle.IsInvalid)
                {
                    throw new ApplicationException("HidCommunication:Open() -> Unable to open a WriteHandle to the device!");
                }
                Debug.WriteLine("HidCommunication:Open() -> Opening successful!---------------------- :)");

                // start async reading
                _usbWriteFileStream = new FileStream(_hidDevice.WriteHandle, FileAccess.Write, _hidDevice.MaxOutputReportLength, false);
                _usbReadFileStream = new FileStream(_hidDevice.ReadHandle, FileAccess.Read, _hidDevice.MaxInputReportLength, true);
                BeginAsyncRead();
                
                _hidDevice.IsOpen = true;
                return true;
            }
            catch (Exception ex)
            {
                Close();
                Debug.WriteLine("HidCommunication:Open() -> " + ex.ToString());
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
                Debug.WriteLine("HidCommunication:Close() -> start closing handles...");
                lock (_usbReadFileStream)
                {
                    Monitor.Pulse(_usbReadFileStream);
                }
                if (_usbReadFileStream != null) { _usbReadFileStream.Close(); }
                if (!_hidDevice.ReadHandle.IsInvalid) { _hidDevice.ReadHandle.Close(); }
                if (!_hidDevice.WriteHandle.IsInvalid) { _hidDevice.WriteHandle.Close(); }
                if (!_hidDevice.HidHandle.IsInvalid) { _hidDevice.HidHandle.Close(); }
                _hidDevice.IsOpen = false;
                Debug.WriteLine("HidCommunication:Close() -> closing complete!----------------");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HidCommunication:Close() -> " + ex.ToString());
            }
        }

        public bool WriteReport(HidOutputReport report)
        {
            // Make sure a device is attached & opened
            if (!_hidDevice.IsAttached | !_hidDevice.IsOpen)
            {
                Debug.WriteLine("HidCommunication:WriteReport(): -> No device attached or Target device is Closed!");
                return false;
            }

            try
            {
                lock (_usbWriteFileStream)
                {
                    _usbWriteFileStream.Write(report.ReportData, 0, report.ReportData.Length);
                }
                return true;
            }
            catch (ArgumentException ex) { Debug.WriteLine("HidCommunication:WriteReport(): -> " + ex.ToString()); return false; }
            catch (IOException ex) { Debug.WriteLine("HidCommunication:WriteReport(): -> " + ex.ToString()); return false; }
        }
        #endregion

        #region Private Methods
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
            catch (ThreadInterruptedException ex) { Debug.WriteLine("HidCommunication:BeginAsyncRead(): -> " + ex.ToString()); }
            catch (SynchronizationLockException ex) { Debug.WriteLine("HidCommunication:BeginAsyncRead(): -> " + ex.ToString()); }
            catch (ArgumentException ex) { Debug.WriteLine("HidCommunication:BeginAsyncRead(): -> " + ex.ToString()); }
            catch (IOException ex) { Debug.WriteLine("HidCommunication:BeginAsyncRead(): -> " + ex.ToString()); }
        }

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
            }
            catch (ThreadInterruptedException ex) { Debug.WriteLine("HidCommunication:AsyncReadCompleted(): -> " + ex.ToString()); }
            catch (SynchronizationLockException ex) { Debug.WriteLine("HidCommunication:AsyncReadCompleted(): -> " + ex.ToString()); }
            catch (ArgumentException ex) { Debug.WriteLine("HidCommunication:AsyncReadCompleted(): -> " + ex.ToString()); }
            catch (InvalidOperationException ex) { Debug.WriteLine("HidCommunication:AsyncReadCompleted(): -> " + ex.ToString()); }
            catch (IOException ex) { Debug.WriteLine("HidCommunication:AsyncReadCompleted(): -> " + ex.ToString()); }
            finally
            {
                // fire the ReportReceived event
                if (ReportReceived != null)
                {
                    ReportReceived(this, new ReportRecievedEventArgs(new HidInputReport { ReportData = reportData }));
                }

                // when all that is done, kick off another read for the next report
                BeginAsyncRead();
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
}