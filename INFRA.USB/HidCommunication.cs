using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using UsbHid.USB.Classes;
using UsbHid.USB.Classes.DllWrappers;

namespace INFRA.USB
{
	/// <summary>
	/// Abstract HID device : Derive your new device controller class from this
	/// </summary>
    internal class HidCommunication : Win32Usb, IDisposable
    {
        #region Public Fields
        #endregion

        #region private / internal Fields

	    private HidDevice _hidDevice;

		/// <summary>Filestream we can use to read/write from</summary>
        private FileStream _usbFileStream;
	    #endregion

        public HidCommunication(ref HidDevice hidDevice)
	    {
            _hidDevice = hidDevice;
	    }

        #region Protected Methods
        /// <summary>
		/// Open the device stream
		/// </summary>
        public bool Open()
        {
            if (!_hidDevice.IsAttached) { return false; }
            if (_hidDevice.IsOpen) { return false; }
            try
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
                    throw new ApplicationException(
                        "usbGenericHidCommunication:findTargetDevice() -> Unable to open a HidHandle to the device!");
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
                    string.Format("usbGenericHidCommunication:findTargetDevice() -> Opening a readHandle to the device"));
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
                    throw new ApplicationException(
                        "usbGenericHidCommunication:findTargetDevice() -> Unable to open a readHandle to the device!");
                }

                Debug.WriteLine(
                    string.Format("usbGenericHidCommunication:findTargetDevice() -> Opening a writeHandle to the device"));
                _hidDevice.WriteHandle = Kernel32.CreateFile(
                    _hidDevice.PathString,
                    Constants.GenericWrite,
                    Constants.FileShareRead | Constants.FileShareWrite,
                    IntPtr.Zero,
                    Constants.OpenExisting, 0, 0);

                // Did we open the writeHandle successfully?
                if (_hidDevice.WriteHandle.IsInvalid)
                {
                    throw new ApplicationException(
                        "usbGenericHidCommunication:findTargetDevice() -> Unable to open a writeHandle to the device!");
                }
                Debug.WriteLine(string.Format("usbGenericHidCommunication:findTargetDevice() -> Opening successful!"));

                // start async reading
                _usbFileStream = new FileStream(_hidDevice.ReadHandle, FileAccess.Read, _hidDevice.MaxInputReportLength,
                    true);
                BeginAsyncRead();
                _hidDevice.IsOpen = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                try
                {
                    _usbFileStream.Close();
                    _hidDevice.ReadHandle.Close();
                    _hidDevice.WriteHandle.Close();
                    _hidDevice.HidHandle.Close();
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Close the device stream
        /// </summary>
        protected void Close()
        {
            try
            {
                _usbFileStream.Close();
                _hidDevice.ReadHandle.Close();
                _hidDevice.WriteHandle.Close();
                _hidDevice.HidHandle.Close();
                _hidDevice.IsOpen = false;
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
            if (report == null || report.Buffer == null) { throw new HIDDeviceException("Null data."); }
            if (report.Buffer.Length > _hidDevice.MaxOutputReportLength) { Array.Resize(ref report.Buffer, _hidDevice.MaxOutputReportLength); }

		    try
            {
                _usbFileStream.Write(report.Buffer, 0, report.Buffer.Length);
                OnDataSent(report);
            }
            catch (IOException ex1)
            {
                Debug.WriteLine(ex1.ToString());
                throw new HIDDeviceException("Device was removed.");
            }
			catch(Exception ex2)
			{
                Debug.WriteLine(ex2.ToString());
                throw new HIDDeviceException("Unknown error.");
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
                _usbFileStream.BeginRead(arrInputReport, 0, _hidDevice.MaxInputReportLength, new AsyncCallback(ReadCompleted), arrInputReport);
            }
            catch (IOException ex1)
            {
                Debug.WriteLine(ex1.ToString());
                throw new HIDDeviceException("Device was removed.");
            }
            catch (Exception ex2)
            {
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
                OnDataReceived(new InputReport(){Buffer = arrBuff});

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
                    if (_usbFileStream != null)
                    {
                        _usbFileStream.Close();
                        _usbFileStream = null;
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
