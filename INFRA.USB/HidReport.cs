using System;

namespace INFRA.USB
{
	/// <summary>
	/// Defines a base class for output reports. To use output reports, just put the bytes into the raw buffer.
	/// </summary>
	public class HidOutputReport
	{
        /// <summary>
        /// Get maximum length that can hold a single report
        /// </summary>
        public static int MaxReportLength { get; internal set; }

        /// <summary>
        /// Get maximum length of Data that can hold a single report
        /// </summary>
        public static int MaxDataLength { get { return MaxReportLength - 1; } }

        /// <summary>
        /// 
        /// </summary>
	    public byte[] ReportData { get; private set; }

        /// <summary>
        /// 
        /// </summary>
	    public byte[] Data
	    {
	        set
	        {
                if (value.Length != MaxDataLength) { Array.Resize(ref value, MaxDataLength); }
                ReportData[0] = 0;  // must be zero
                Array.Copy(value, 0, ReportData, 1, MaxDataLength);
	        }
	    }

	    /// <summary>
	    /// Construction. Setup the buffer with the correct output report length dictated by the device
	    /// </summary>
        public HidOutputReport()
	    {
	        ReportData = new byte[MaxReportLength];
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
        internal static int MaxReportLength { get; set; }

        /// <summary>
        /// Get maximum length of Data that can hold a single report
        /// </summary>
        public static int MaxDataLength { get { return MaxReportLength - 1; } }

        /// <summary>
        /// 
        /// </summary>
        public byte[] ReportData
	    {
	        set
	        {
                if (value.Length != MaxReportLength) { Array.Resize(ref value, MaxReportLength); }
                Array.Copy(value, 1, Data, 0, MaxReportLength - 1);
	        }
	    }

        /// <summary>
        /// 
        /// </summary>
        public byte[] Data { get; private set; }

	    /// <summary>
	    /// Construction. Setup the buffer with the correct output report length dictated by the device
	    /// </summary>
        public HidInputReport()
	    {
            Data = new byte[MaxReportLength - 1];
	    }
	}
}
