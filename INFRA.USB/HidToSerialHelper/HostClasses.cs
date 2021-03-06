﻿using System;
using INFRA.USB.HidHelper;

namespace INFRA.USB.HidToSerialHelper
{
    // ReSharper disable CSharpWarnings::CS1591
    // ReSharper disable InconsistentNaming

    public abstract class HostPacket
    {
        protected const int TRANSMISSION_TYPE_INDEX = 0;
        protected const int MAX_SIZE_OF_ACK_BYTE = 255;
        protected const int MAX_SIZE_OF_TIMEOUT = 65535;

        public HostTransmisionType TransmisionType { get; protected set; }

        public HidOutputReport ReportToSend { get; protected set; }

        public byte[] RawData
        {
            get { return ReportToSend.UserData; }
        }

        protected HostPacket()
        {
            ReportToSend = new HidOutputReport();
        }

        protected void Validate(int value, int max, string errMsg = "Invalid range.")
        {
            if (value < 0 || value > max)
                throw new ArgumentOutOfRangeException(errMsg);
        }

        public abstract void GenerateReportData();
    }

    public class BaudRateCommand_FromHost : HostPacket
    {
        protected const int BAUD_RATE_INDEX = 1;
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
                    throw new ApplicationException("value must be 1, 2, 3, 4, 9, 14, 19, 38, 56, 57, 115 or 128");
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

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
            ReportToSend.UserData[BAUD_RATE_INDEX] = (byte)BaudRateIndex;
        }
    }

    public class SingleQuery_FromHost : HostPacket
    {
        protected const int SEGMENT_LENGTH_INDEX = 1;
        protected const int EXPECTED_DATA_LENGTH_INDEX = 2;
        protected const int TIMEOUT_INDEX = 3;
        protected const int DATA_INDEX = 5;
        protected const int MAX_DATA_LENGTH = 59;
        protected const int MAX_EXPECTED_DATA_LENGTH = 62;

        private int _thisSegmentDataLength;
        private int _expectedDataLength;
        private int _timeout;

        public SingleQuery_FromHost()
        {
            TransmisionType = HostTransmisionType.SINGLE_QUERY_FROM_HOST;
            _thisSegmentDataLength = 0;
            _expectedDataLength = 0;
            _timeout = 0;
            Data = new byte[MAX_DATA_LENGTH];
        }

        /// <summary>
        /// Max: 59
        /// </summary>
        public int ThisSegmentDataLength
        {
            set
            {
                Validate(value, MAX_DATA_LENGTH);
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
                Validate(value, MAX_EXPECTED_DATA_LENGTH);
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
        public byte[] Data { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">Max: 59</param>
        public void SetData(byte[] source, int startIndex, int length)
        {
            Validate(length, MAX_DATA_LENGTH, "Data size is too large to fit.");
            _thisSegmentDataLength = length;
            Array.Copy(source, startIndex, Data, 0, length);
        }

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
            ReportToSend.UserData[SEGMENT_LENGTH_INDEX] = (byte)ThisSegmentDataLength;
            ReportToSend.UserData[EXPECTED_DATA_LENGTH_INDEX] = (byte)ExpectedDataLength;
            ReportToSend.UserData[TIMEOUT_INDEX] = BitConverter.GetBytes(Timeout)[0];
            ReportToSend.UserData[TIMEOUT_INDEX + 1] = BitConverter.GetBytes(Timeout)[1];
            Array.Copy(Data, 0, ReportToSend.UserData, DATA_INDEX, _thisSegmentDataLength);
        }
    }

    public class SyncOutData_FromHost : HostPacket
    {
        protected const int SEGMENT_LENGTH_INDEX = 1;
        protected const int REMAINING_PACKETS_LENGTH_INDEX = 2;
        protected const int LAST_PACKET_LENGTH_INDEX = 4;
        protected const int DEVICE_ACK_BYTE_INDEX = 5;
        protected const int DATA_INDEX = 6;
        protected const int MAX_DATA_LENGTH = 58;
        protected const int MAX_SIZE_OF_REMAINING_PACKET = 65535;

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
                Validate(value, MAX_DATA_LENGTH);
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
                Validate(value, MAX_SIZE_OF_REMAINING_PACKET);
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
                Validate(value, MAX_DATA_LENGTH);
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
                Validate(value.Length, MAX_DATA_LENGTH, "Array size is too large to fit.");
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
            Data = new byte[MAX_DATA_LENGTH];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">Max: 59</param>
        public void SetData(byte[] source, int startIndex, int length)
        {
            Validate(length, MAX_DATA_LENGTH, "Data size is too large to fit.");
            _thisSegmentDataLength = length;
            Array.Copy(source, startIndex, Data, 0, length);
        }

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
            ReportToSend.UserData[SEGMENT_LENGTH_INDEX] = (byte)ThisSegmentDataLength;
            ReportToSend.UserData[REMAINING_PACKETS_LENGTH_INDEX] = BitConverter.GetBytes(NoOfRemainingPackets)[0];
            ReportToSend.UserData[REMAINING_PACKETS_LENGTH_INDEX + 1] = BitConverter.GetBytes(NoOfRemainingPackets)[1];
            ReportToSend.UserData[LAST_PACKET_LENGTH_INDEX] = (byte)LastPacketLength;
            ReportToSend.UserData[DEVICE_ACK_BYTE_INDEX] = (byte)DeviceAckByte;
            Array.Copy(Data, 0, ReportToSend.UserData, DATA_INDEX, _thisSegmentDataLength);
        }
    }

    public class SyncInStart_FromHost : HostPacket
    {
        protected const int TIMEOUT_INDEX = 1;
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

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
            ReportToSend.UserData[TIMEOUT_INDEX] = BitConverter.GetBytes(Timeout)[0];
            ReportToSend.UserData[TIMEOUT_INDEX + 1] = BitConverter.GetBytes(Timeout)[1];
        }
    }

    public class SyncInRead_FromHost : HostPacket
    {
        public SyncInRead_FromHost()
        {
            TransmisionType = HostTransmisionType.SYNC_IN_READ_FROM_HOST;
        }

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
        }
    }

    public class SyncInAck_FromHost : HostPacket
    {
        protected const int HOST_ACK_BYTE_INDEX = 1;

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

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
            ReportToSend.UserData[HOST_ACK_BYTE_INDEX] = (byte)HostAckByte;
        }
    }

    public class AsyncOut_FromHost : HostPacket
    {
        protected const int SEGMENT_LENGTH_INDEX = 1;
        protected const int DATA_INDEX = 2;
        protected const int MAX_DATA_LENGTH = 62;

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
                Validate(value, MAX_DATA_LENGTH);
                _dataLength = value;
            }
            get { return _dataLength; }
        }

        public byte[] Data
        {
            set
            {
                Validate(value.Length, MAX_DATA_LENGTH, "Array size is too large to fit.");
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
            Validate(length, MAX_DATA_LENGTH, "Data size is too large to fit.");
            _dataLength = length;
            Array.Copy(source, startIndex, Data, 0, length);
        }

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
            ReportToSend.UserData[SEGMENT_LENGTH_INDEX] = (byte)ThisSegmentDataLength;
            Array.Copy(Data, 0, ReportToSend.UserData, DATA_INDEX, _dataLength);
        }
    }

    public class AsyncInStart_FromHost : HostPacket
    {
        protected const int REQIRED_DATA_LENGTH_INDEX = 1;
        protected const int TIMEOUT_INDEX = 2;
        protected const int MAX_EXPECTED_DATA_LENGTH = 62;

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
                Validate(value, MAX_EXPECTED_DATA_LENGTH);
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

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
            ReportToSend.UserData[REQIRED_DATA_LENGTH_INDEX] = (byte)RequiredDataLength;
            ReportToSend.UserData[TIMEOUT_INDEX] = BitConverter.GetBytes(Timeout)[0];
            ReportToSend.UserData[TIMEOUT_INDEX + 1] = BitConverter.GetBytes(Timeout)[1];
        }
    }

    public class AsyncInStop_FromHost : HostPacket
    {
        public AsyncInStop_FromHost()
        {
            TransmisionType = HostTransmisionType.ASYNC_IN_STOP_FROM_HOST;
        }

        public override void GenerateReportData()
        {
            ReportToSend.UserData[TRANSMISSION_TYPE_INDEX] = (byte)TransmisionType;
        }
    } 
}
