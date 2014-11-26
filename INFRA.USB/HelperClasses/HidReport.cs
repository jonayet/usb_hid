using System;

namespace INFRA.USB.HelperClasses
{
	/// <summary>
	/// Defines a base class for output reports. To use output reports, just put the bytes into the raw buffer.
	/// </summary>
	public class HidOutputReport
	{
        /// <summary>
        /// Get maximum length that can hold a single report
        /// </summary>
        public static int ReportDataLength { get; internal set; }

        /// <summary>
        /// Get maximum length of Data that can hold a single report
        /// </summary>
        public static int UserDataLength { get { return ReportDataLength - 1; } }

	    /// <summary>
	    /// 
	    /// </summary>
        public byte[] ReportData
	    {
	        get
	        {
	            _reportData[0] = 0; // must be zero!
                Array.Copy(_userData, 0, _reportData, 1, UserDataLength);
	            return _reportData;
	        }
	    }

        /// <summary>
        /// 
        /// </summary>
	    public byte[] UserData
	    {
	        set
	        {
                if (value.Length != UserDataLength) { Array.Resize(ref value, UserDataLength); }
	            _userData = value;
	        }
            get { return _userData; }
	    }

        private byte[] _userData;
        private readonly byte[] _reportData;

	    /// <summary>
	    /// Construction. Setup the buffer with the correct output report length dictated by the device
	    /// </summary>
        public HidOutputReport()
	    {
            _reportData = new byte[ReportDataLength];
	    }
	}

	/// <summary>
	/// Defines a base class for input reports. To use input reports, use the SetData method and override the 
	/// ProcessData method.
	/// </summary>
	public class HidInputReport
	{
        /// <summary>
        /// Get maximum length that can hold a single report
        /// </summary>
        public static int ReportDataLength { get; set; }

        /// <summary>
        /// Get maximum length of Data that can hold a single report
        /// </summary>
        public static int UserDataLength { get { return ReportDataLength - 1; } }

        /// <summary>
        /// 
        /// </summary>
        public byte[] ReportData
	    {
            set
            {
                if (value.Length != ReportDataLength) { Array.Resize(ref value, ReportDataLength); }
                _reportData = value;
            }
            get { return _reportData; }
	    }

	    /// <summary>
	    /// 
	    /// </summary>
	    public byte[] UserData
	    {
	        get
	        {
                Array.Copy(_reportData, 1, _userData, 0, UserDataLength);
	            return _userData;
	        }
	    }

        private readonly byte[] _userData;
        private byte[] _reportData;

	    /// <summary>
	    /// Construction. Setup the buffer with the correct output report length dictated by the device
	    /// </summary>
        public HidInputReport()
	    {
            _userData = new byte[UserDataLength];
	    }
	}
}
