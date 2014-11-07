using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace INFRA.USB
{
	/// <summary>
	/// Abstract HID device : Derive your new device controller class from this
	/// </summary>
    public abstract class BasicHidDevice : Win32Usb, IDisposable
    {
        #region private / internal Fields

        /// <summary>Product Version</summary>
        public ushort ProductVersion { get; private set; }

        /// <summary>Handle to the device</summary>
        public IntPtr DeviceHandle { get; private set; }

        /// <summary>Accessor for output report length</summary>
        public int MaxOutputReportLength { get; private set; }

        /// <summary>Accessor for input report length</summary>
        public int MaxInputReportLength { get; private set; }

        /// <summary>IsInitialized</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>IsConnected</summary>
        public bool IsConnected { get; private set; }
        #endregion

        #region Privates Fields

		/// <summary>Filestream we can use to read/write from</summary>
        private FileStream _usbFileStream;
	    #endregion

        #region Constructor
        public BasicHidDevice()
        {
            IsInitialized = false;
        } 
        #endregion

        #region Protected Methods
        /// <summary>
		/// Initialises the device
		/// </summary>
		/// <param name="devicePath">Path to the device.</param>
        protected void Initialize(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath)) { return; }
            IsConnected = false;
            try
            {
                // Create the file from the device path
                DeviceHandle = CreateFile(devicePath, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING,
                        FILE_FLAG_OVERLAPPED, IntPtr.Zero);

                // if the open failed...
                if (DeviceHandle == InvalidHandleValue)
                {
                    DeviceHandle = IntPtr.Zero;
                    throw HIDDeviceException.GenerateWithWinError("Failed to create device file");
                }

                if (!GetAttributes())
                {
                    DeviceHandle = IntPtr.Zero;
                    throw HIDDeviceException.GenerateWithWinError("Failed to get Attributes");
                }

                if (!GetCapabilities())
                {
                    DeviceHandle = IntPtr.Zero;
                    throw HIDDeviceException.GenerateWithWinError("Failed to get Capabilities");
                }

                if (_usbFileStream == null)
                {
                    // create file stream from the handle
                    _usbFileStream = new FileStream(new SafeFileHandle(DeviceHandle, false),
                        FileAccess.Read | FileAccess.Write, MaxInputReportLength, true);
                }

                // set IsConnected flag
                IsConnected = true;
                IsInitialized = true;

                // kick off the first asynchronous read                              
                BeginAsyncRead();
            }
            catch (Exception ex)
            {
                DeviceHandle = IntPtr.Zero;
                Debug.WriteLine(ex.ToString());
            }
		}

		/// <summary>
		/// Write an output report to the device.
		/// </summary>
        /// <param name="report">Output report to write</param>
        protected void Write(OutputReport report)
        {
            if (report == null || report.Buffer == null) { throw new HIDDeviceException("Null data."); }
            if (report.Buffer.Length > MaxOutputReportLength) { Array.Resize(ref report.Buffer, MaxOutputReportLength); }

		    try
            {
                _usbFileStream.Write(report.Buffer, 0, report.Buffer.Length);
                OnDataSent(report);
            }
            catch (IOException ex1)
            {
                IsConnected = false;
                Debug.WriteLine(ex1.ToString());
                throw new HIDDeviceException("Device was removed.");
            }
			catch(Exception ex2)
			{
                IsConnected = false;
                Debug.WriteLine(ex2.ToString());
                throw new HIDDeviceException("Unknown error.");
			}
        }
        #endregion

        #region Private Methods

        internal bool GetAttributes()
	    {
            var attributes = new HIDD_ATTRIBUTES();
            attributes.Size = Marshal.SizeOf(attributes);
            if (!HidD_GetAttributes(DeviceHandle, ref attributes)) { return false; }
            ProductVersion = (ushort)attributes.VersionNumber;
            return true;
	    }

        internal bool GetCapabilities()
        {
            // get windows to read the device data into an internal buffer
            IntPtr lpData;
            if (!HidD_GetPreparsedData(DeviceHandle, out lpData)) { return false; }
            
            // extract the device capabilities from the internal buffer
            HidCaps oCaps;
            HidP_GetCaps(lpData, out oCaps);
            MaxInputReportLength = oCaps.InputReportByteLength;
            MaxOutputReportLength = oCaps.OutputReportByteLength;

            // before we quit the funtion, we must free the internal buffer reserved in GetPreparsedData
            HidD_FreePreparsedData(ref lpData);
            return true;
        }

        /// <summary>
        /// Kicks off an asynchronous read which completes when data is read or when the device
        /// is disconnected. Uses a callback.
        /// </summary>
        private void BeginAsyncRead()
        {
            var arrInputReport = new byte[MaxInputReportLength];
            try
            {
                // put the buff we used to receive the stuff as the async state then we can get at it when the read completes
                _usbFileStream.BeginRead(arrInputReport, 0, MaxInputReportLength, new AsyncCallback(ReadCompleted), arrInputReport);
            }
            catch (IOException ex1)
            {
                IsConnected = false;
                Debug.WriteLine(ex1.ToString());
                throw new HIDDeviceException("Device was removed.");
            }
            catch (Exception ex2)
            {
                IsConnected = false;
                Debug.WriteLine(ex2.ToString());
                throw new HIDDeviceException("Unknown error.");
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
                _usbFileStream.EndRead(iResult);
                //OnDataReceived(new InputReport(){Buffer = arrBuff});

                // when all that is done, kick off another read for the next report
                BeginAsyncRead();
            }
            catch (IOException ex1)
            {
                IsConnected = false;
                CloseHandle(DeviceHandle);
                Debug.WriteLine(ex1.ToString());
                //throw new HIDDeviceException("Device was removed.");
            }
            catch (Exception ex2)
            {
                CloseHandle(DeviceHandle);
                IsConnected = false;
                Debug.WriteLine(ex2.ToString());
                throw new HIDDeviceException("Unknown error.");
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
                    if (_usbFileStream != null)
                    {
                        _usbFileStream.Close();
                        _usbFileStream = null;
                    }
                }
                if (DeviceHandle != IntPtr.Zero)	// Dispose and finalize, get rid of unmanaged resources
                {
                    CloseHandle(DeviceHandle);
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
    public class HIDDeviceException : ApplicationException
    {
        public HIDDeviceException(string strMessage) : base(strMessage) { }

        public static HIDDeviceException GenerateWithWinError(string strMessage)
        {
            return new HIDDeviceException(string.Format("Msg:{0} WinEr:{1:X8}", strMessage, Marshal.GetLastWin32Error()));
        }

        public static HIDDeviceException GenerateError(string strMessage)
        {
            return new HIDDeviceException(string.Format("Msg:{0}", strMessage));
        }
    }
    #endregion
}
