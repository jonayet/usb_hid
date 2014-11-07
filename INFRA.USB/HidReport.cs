using System;

namespace INFRA.USB
{
	/// <summary>
	/// Base class for report types. Simply wraps a byte buffer.
	/// </summary>
	public abstract class Report
	{
		public byte[] Buffer;

        /// <summary>
        /// Construction. Setup the buffer with the correct output report length dictated by the device
        /// </summary>
        /// <param name="length">Length of the Report</param>
	    protected Report(int length)
		{
            if (length > 0)
            {
                Buffer = new byte[length];
            }
		}
	}

	/// <summary>
	/// Defines a base class for output reports. To use output reports, just put the bytes into the raw buffer.
	/// </summary>
	public class OutputReport : Report
	{
	    /// <summary>
	    /// Construction. Setup the buffer with the correct output report length dictated by the device
	    /// </summary>
        /// <param name="length">Length of the Report</param>
        public OutputReport(int length) : base(length)
		{
		}
	}

	/// <summary>
	/// Defines a base class for input reports. To use input reports, use the SetData method and override the 
	/// ProcessData method.
	/// </summary>
	public class InputReport : Report
	{
		/// <summary>
        /// InputReport Construction.
		/// </summary>
		public InputReport() : base(0)
		{

		}
	}
}
