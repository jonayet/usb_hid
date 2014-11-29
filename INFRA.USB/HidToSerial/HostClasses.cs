using System;
using INFRA.USB.HelperClasses;

namespace INFRA.USB.HidToSerial
{
    public abstract class HostPacket
    {
        protected const int SINGLE_QUERY_MAX_SIZE_OF_DATA = 59;
        protected const int SINGLE_QUERY_MAX_SIZE_OF_RESPONSE = 62;
        protected const int SYNC_OUT_MAX_SIZE_OF_DATA = 58;
        protected const int SYNC_OUT_MAX_SIZE_OF_LAST_PACKET = 58;
        protected const int SYNC_OUT_MAX_SIZE_OF_REMAINING_PACKET = 65535;
        protected const int ASYNC_OUT_MAX_SIZE_OF_DATA = 62;
        protected const int ASYNC_IN_START_MAX_SIZE_OF_DATA = 62;
        protected const int MAX_SIZE_OF_ACK_BYTE = 255;
        protected const int MAX_SIZE_OF_TIMEOUT = 65535;

        public HostTransmisionType TransmisionType { get; protected set; }
        public HidOutputReport ReportToSend { get; protected set; }

        public byte[] RawData
        {
            get { ProcessRawData(); return ReportToSend.UserData; }
        }

        protected HostPacket()
        {
            ReportToSend = new HidOutputReport();
        }

        protected virtual void ProcessRawData()
        {

        }

        protected void Validate(int value, int max, string errMsg = "Invalid range.")
        {
            if (value < 0 || value > max)
                throw new ArgumentOutOfRangeException(errMsg);
        }
    }

    public class BaudRateCommand_FromHost : HostPacket
    {
        private int _baudRate;

        /// <summary>
        /// index=1 > Baudrate : 1200; Index=2 > Baudrate : 2400; index=4 > Baudrate : 4800;
        /// index=9 > Baudrate : 9600; Index=14 > Baudrate : 14400; index=19 > Baudrate : 19200;
        /// index=38 > Baudrate : 38400; index=56 > Baudrate : 56000; index=57 > Baudrate : 57600;
        /// Index=115 > Baudrate : 115200; index=128 > Baudrate : 128000;
        /// </summary>
        public int BaudRateIndex
        {
            set
            {
                if (value != 1 && value != 2 && value != 4 && value != 9 && value != 14 && value != 19 && value != 38 && value != 56 && value != 57 && value != 115 && value != 128)
                {
                    throw new ArgumentOutOfRangeException("value must be 1, 2, 3, 4, 9, 14, 19, 38, 56, 57, 115 or 128");
                }
                _baudRate = value;
            }
            get { return _baudRate; }
        }

        public BaudRateCommand_FromHost()
        {
            TransmisionType = HostTransmisionType.BAUDRATE_CMD_FROM_HOST;
            _baudRate = 9;
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
            ReportToSend.UserData[1] = (byte)BaudRateIndex;
        }
    }

    public class SingleQuery_FromHost : HostPacket
    {
        private int _thisSegmentDataLength;
        private int _expectedDataLength;
        private int _timeout;

        public SingleQuery_FromHost()
        {
            TransmisionType = HostTransmisionType.SINGLE_QUERY_FROM_HOST;
            _thisSegmentDataLength = 0;
            _expectedDataLength = 0;
            _timeout = 0;
            Data = new byte[SINGLE_QUERY_MAX_SIZE_OF_DATA];
        }

        /// <summary>
        /// Max: 59
        /// </summary>
        public int ThisSegmentDataLength
        {
            set
            {
                Validate(value, SINGLE_QUERY_MAX_SIZE_OF_DATA);
                _thisSegmentDataLength = value;
            }
            get { return _thisSegmentDataLength; }
        }

        /// <summary>
        /// Max: 62
        /// </summary>
        public int ExpectedDataLength
        {
            set
            {
                Validate(value, SINGLE_QUERY_MAX_SIZE_OF_RESPONSE);
                _expectedDataLength = value;
            }
            get { return _expectedDataLength; }
        }

        /// <summary>
        /// Max: 65535
        /// </summary>
        public int Timeout
        {
            set
            {
                Validate(value, MAX_SIZE_OF_TIMEOUT);
                _timeout = value;
            }
            get { return _timeout; }
        }

        /// <summary>
        /// Max Length: 59
        /// </summary>
        public byte[] Data
        {
            private set
            {
                Validate(value.Length, SINGLE_QUERY_MAX_SIZE_OF_DATA, "Array size is too large to fit.");
                ReportToSend.UserData = value;
                _thisSegmentDataLength = value.Length;
            }
            get { return ReportToSend.UserData; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">Max: 59</param>
        public void SetData(byte[] source, int startIndex, int length)
        {
            Validate(length, SINGLE_QUERY_MAX_SIZE_OF_DATA, "Data size is too large to fit.");
            _thisSegmentDataLength = length;
            for (var i = 0; i < length; i++)
            {
                Data[i] = source[startIndex + i];
            }
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
            ReportToSend.UserData[1] = (byte)ThisSegmentDataLength;
            ReportToSend.UserData[2] = (byte)ExpectedDataLength;
            ReportToSend.UserData[3] = BitConverter.GetBytes(Timeout)[0];
            ReportToSend.UserData[4] = BitConverter.GetBytes(Timeout)[1];
            for (int i = 0; i < _thisSegmentDataLength; i++)
            {
                ReportToSend.UserData[5 + i] = Data[i];
            }
        }
    }

    public class SyncOutData_FromHost : HostPacket
    {
        private int _thisSegmentDataLength;
        private int _noOfRemainingPackets;
        private int _lastPacketLength;
        private int _deviceAckByte;

        /// <summary>
        /// Max: 58
        /// </summary>
        public int ThisSegmentDataLength
        {
            set
            {
                Validate(value, SYNC_OUT_MAX_SIZE_OF_DATA);
                _thisSegmentDataLength = value;
            }
            get { return _thisSegmentDataLength; }
        }

        /// <summary>
        /// Max: 65535
        /// </summary>
        public int NoOfRemainingPackets
        {
            set
            {
                Validate(value, SYNC_OUT_MAX_SIZE_OF_REMAINING_PACKET);
                _noOfRemainingPackets = value;
            }
            get { return _noOfRemainingPackets; }
        }

        /// <summary>
        /// Max: 58
        /// </summary>
        public int LastPacketLength
        {
            set
            {
                Validate(value, SYNC_OUT_MAX_SIZE_OF_LAST_PACKET);
                _lastPacketLength = value;
            }
            get { return _lastPacketLength; }
        }

        /// <summary>
        /// 8 bit
        /// </summary>
        public int DeviceAckByte
        {
            set
            {
                Validate(value, MAX_SIZE_OF_ACK_BYTE);
                _deviceAckByte = value;
            }
            get { return _deviceAckByte; }
        }

        public byte[] Data
        {
            private set
            {
                Validate(value.Length, SYNC_OUT_MAX_SIZE_OF_DATA, "Array size is too large to fit.");
                ReportToSend.UserData = value;
                _thisSegmentDataLength = value.Length;
            }
            get { return ReportToSend.UserData; }
        }

        public SyncOutData_FromHost()
        {
            TransmisionType = HostTransmisionType.SYNC_OUT_DATA_FROM_HOST;
            _thisSegmentDataLength = 0;
            _lastPacketLength = 0;
            _noOfRemainingPackets = 0;
            _deviceAckByte = 0;
            Data = new byte[SYNC_OUT_MAX_SIZE_OF_DATA];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">Max: 59</param>
        public void SetData(byte[] source, int startIndex, int length)
        {
            Validate(length, SYNC_OUT_MAX_SIZE_OF_DATA, "Data size is too large to fit.");
            _thisSegmentDataLength = length;
            for (var i = 0; i < length; i++)
            {
                Data[i] = source[startIndex + i];
            }
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
            ReportToSend.UserData[1] = (byte)ThisSegmentDataLength;
            ReportToSend.UserData[2] = BitConverter.GetBytes(NoOfRemainingPackets)[0];
            ReportToSend.UserData[3] = BitConverter.GetBytes(NoOfRemainingPackets)[1];
            ReportToSend.UserData[4] = (byte)LastPacketLength;
            ReportToSend.UserData[5] = (byte)DeviceAckByte;
            for (int i = 0; i < _thisSegmentDataLength; i++)
            {
                ReportToSend.UserData[6 + i] = Data[i];
            }
        }
    }

    public class SyncInStart_FromHost : HostPacket
    {
        private int _timeout;

        /// <summary>
        /// Max: 65535
        /// </summary>
        public int Timeout
        {
            set
            {
                Validate(value, MAX_SIZE_OF_TIMEOUT);
                _timeout = value;
            }
            get { return _timeout; }
        }

        public SyncInStart_FromHost()
        {
            _timeout = 0;
            TransmisionType = HostTransmisionType.SYNC_IN_START_FROM_HOST;
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
            ReportToSend.UserData[1] = BitConverter.GetBytes(Timeout)[0];
            ReportToSend.UserData[2] = BitConverter.GetBytes(Timeout)[1];
        }
    }

    public class SyncInRead_FromHost : HostPacket
    {
        public SyncInRead_FromHost()
        {
            TransmisionType = HostTransmisionType.SYNC_IN_READ_FROM_HOST;
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
        }
    }

    public class SyncInAck_FromHost : HostPacket
    {
        private int _hostAckByte;

        /// <summary>
        /// 8 bit
        /// </summary>
        public int HostAckByte
        {
            set
            {
                Validate(value, MAX_SIZE_OF_ACK_BYTE);
                _hostAckByte = value;
            }
            get { return _hostAckByte; }
        }

        public SyncInAck_FromHost()
        {
            TransmisionType = HostTransmisionType.SYNC_IN_ACK_FROM_HOST;
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
            ReportToSend.UserData[1] = (byte)HostAckByte;
        }
    }

    public class AsyncOut_FromHost : HostPacket
    {
        private int _dataLength;
        public AsyncOut_FromHost()
        {
            TransmisionType = HostTransmisionType.ASYNC_OUT_FROM_HOST;
        }

        /// <summary>
        /// Max: 62
        /// </summary>
        public int ThisSegmentDataLength
        {
            set
            {
                Validate(value, ASYNC_OUT_MAX_SIZE_OF_DATA);
                _dataLength = value;
            }
            get { return _dataLength; }
        }

        public byte[] Data
        {
            private set
            {
                Validate(value.Length, ASYNC_OUT_MAX_SIZE_OF_DATA, "Array size is too large to fit.");
                ReportToSend.UserData = value;
                _dataLength = value.Length;
            }
            get { return ReportToSend.UserData; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">Max: 62</param>
        public void SetData(byte[] source, int startIndex, int length)
        {
            Validate(length, ASYNC_OUT_MAX_SIZE_OF_DATA, "Data size is too large to fit.");
            _dataLength = length;
            for (var i = 0; i < length; i++)
            {
                Data[i] = source[startIndex + i];
            }
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
            ReportToSend.UserData[1] = (byte)ThisSegmentDataLength;
            for (int i = 0; i < _dataLength; i++)
            {
                ReportToSend.UserData[2 + i] = Data[i];
            }
        }
    }

    public class AsyncInStart_FromHost : HostPacket
    {
        private int _requiredDataLength;
        private int _timeout;
        public AsyncInStart_FromHost()
        {
            TransmisionType = HostTransmisionType.ASYNC_IN_START_FROM_HOST;
            _requiredDataLength = 0;
            _timeout = 0;
        }

        /// <summary>
        /// Max: 62
        /// </summary>
        public int RequiredDataLength
        {
            set
            {
                Validate(value, ASYNC_IN_START_MAX_SIZE_OF_DATA);
                _requiredDataLength = value;
            }
            get { return _requiredDataLength; }
        }

        /// <summary>
        /// Max: 65535
        /// </summary>
        public int Timeout
        {
            set
            {
                Validate(value, MAX_SIZE_OF_TIMEOUT);
                _timeout = value;
            }
            get { return _timeout; }
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
            ReportToSend.UserData[1] = (byte)RequiredDataLength;
            ReportToSend.UserData[2] = BitConverter.GetBytes(Timeout)[0];
            ReportToSend.UserData[3] = BitConverter.GetBytes(Timeout)[1];
        }
    }

    public class AsyncInStop_FromHost : HostPacket
    {
        public AsyncInStop_FromHost()
        {
            TransmisionType = HostTransmisionType.ASYNC_IN_STOP_FROM_HOST;
        }

        protected override void ProcessRawData()
        {
            ReportToSend.UserData[0] = (byte)TransmisionType;
        }
    } 
}
