using System;
using INFRA.USB.HelperClasses;

namespace INFRA.USB.HidToSerial
{
    // ReSharper disable CSharpWarnings::CS1591
    // ReSharper disable InconsistentNaming

    public abstract class DevicePacket
    {
        protected const int TRANSMISSION_TYPE_INDEX = 0;

        public HidInputReport ReportReceived
        {
            internal set { _rawReport = value; ProcessRawReport(); }
            get { return _rawReport; }
        }
        public DeviceTransmisionType TransmisionType { get; protected set; }

        private HidInputReport _rawReport;

        protected DevicePacket()
        {
            _rawReport = new HidInputReport();
        }

        protected abstract void ProcessRawReport();
    }

    public class BaudRateResponse_FromDevice : DevicePacket
    {
        protected const int DATA_INDEX = 1;
        protected const int MAX_DATA_LENGTH = 40;
        public byte[] Data { get; private set; }

        public BaudRateResponse_FromDevice()
        {
            TransmisionType = DeviceTransmisionType.NONE_FROM_DEVICE;
            Data = new byte[0];
        }

        protected override void ProcessRawReport()
        {
            TransmisionType = (DeviceTransmisionType)ReportReceived.UserData[TRANSMISSION_TYPE_INDEX];
            Data = new byte[MAX_DATA_LENGTH];
            Array.Copy(ReportReceived.UserData, DATA_INDEX, Data, 0, MAX_DATA_LENGTH);
        }
    }

    public class SingleResponse_FromDevice : DevicePacket
    {
        protected const int SEGMENT_LENGTH_INDEX = 1;
        protected const int DATA_INDEX = 2;
        protected const int MAX_DATA_LENGTH = 62;

        public int ThisSegmentDataLength { get; protected set; }
        public byte[] Data { get; private set; }

        public SingleResponse_FromDevice()
        {
            TransmisionType = DeviceTransmisionType.NONE_FROM_DEVICE;
            ThisSegmentDataLength = 0;
            Data = new byte[0];
        }

        protected override void ProcessRawReport()
        {
            TransmisionType = (DeviceTransmisionType)ReportReceived.UserData[TRANSMISSION_TYPE_INDEX];
            ThisSegmentDataLength = ReportReceived.UserData[SEGMENT_LENGTH_INDEX];
            Data = new byte[ThisSegmentDataLength];
            Array.Copy(ReportReceived.UserData, DATA_INDEX, Data, 0, ThisSegmentDataLength);
        }
    }

    public class SyncOutAck_FromDevice : DevicePacket
    {
        protected const int DEVICE_ACK_BYTE_INDEX = 1;

        public int DeviceAckByte { get; protected set; }

        public SyncOutAck_FromDevice()
        {
            TransmisionType = DeviceTransmisionType.NONE_FROM_DEVICE;
            DeviceAckByte = 0;
        }

        protected override void ProcessRawReport()
        {
            TransmisionType = (DeviceTransmisionType)ReportReceived.UserData[TRANSMISSION_TYPE_INDEX];
            DeviceAckByte = ReportReceived.UserData[DEVICE_ACK_BYTE_INDEX];
        }
    }

    public class SyncInData_FromDevice : DevicePacket
    {
        protected const int SEGMENT_LENGTH_INDEX = 1;
        protected const int REMAINING_PACKETS_LENGTH_INDEX = 2;
        protected const int LAST_PACKET_LENGTH_INDEX = 3;
        protected const int HOST_ACK_BYTE_INDEX = 4;
        protected const int DATA_INDEX = 5;
        protected const int MAX_DATA_LENGTH = 59;
        
        public int ThisSegmentDataLength { get; protected set; }
        public int NoOfRemainingPackets { get; protected set; }
        public int LastPacketLength { get; protected set; }
        public int HostAckByte { get; protected set; }
        public byte[] Data { get; protected set; }

        public SyncInData_FromDevice()
        {
            TransmisionType = DeviceTransmisionType.NONE_FROM_DEVICE;
            ThisSegmentDataLength = 0;
            NoOfRemainingPackets = 0;
            LastPacketLength = 0;
            HostAckByte = 0;
            Data = new byte[0];
        }

        protected override void ProcessRawReport()
        {
            TransmisionType = (DeviceTransmisionType)ReportReceived.UserData[TRANSMISSION_TYPE_INDEX];
            ThisSegmentDataLength = ReportReceived.UserData[SEGMENT_LENGTH_INDEX];
            NoOfRemainingPackets = ReportReceived.UserData[REMAINING_PACKETS_LENGTH_INDEX];
            LastPacketLength = ReportReceived.UserData[LAST_PACKET_LENGTH_INDEX];
            HostAckByte = ReportReceived.UserData[HOST_ACK_BYTE_INDEX];
            Data = new byte[ThisSegmentDataLength];
            Array.Copy(ReportReceived.UserData, DATA_INDEX, Data, 0, ThisSegmentDataLength);
        }
    }

    public class AsyncInData_FromDevice : DevicePacket
    {
        protected const int SEGMENT_LENGTH_INDEX = 1;
        protected const int DATA_INDEX = 2;
        protected const int MAX_DATA_LENGTH = 62;

        public int ThisSegmentDataLength { get; protected set; }
        public byte[] Data { get; protected set; }

        public AsyncInData_FromDevice()
        {
            TransmisionType = DeviceTransmisionType.NONE_FROM_DEVICE;
            ThisSegmentDataLength = 0;
            Data = new byte[0];
        }

        protected override void ProcessRawReport()
        {
            TransmisionType = (DeviceTransmisionType)ReportReceived.UserData[TRANSMISSION_TYPE_INDEX];
            ThisSegmentDataLength = ReportReceived.UserData[SEGMENT_LENGTH_INDEX];
            Data = new byte[ThisSegmentDataLength];
            Array.Copy(ReportReceived.UserData, DATA_INDEX, Data, 0, ThisSegmentDataLength);
        }
    }

    public class UnknownResponse_FromDevice : DevicePacket
    {
        protected const int DATA_INDEX = 1;
        protected const int MAX_DATA_LENGTH = 10;

        public byte[] Data { get; protected set; }

        public UnknownResponse_FromDevice()
        {
            TransmisionType = DeviceTransmisionType.NONE_FROM_DEVICE;
            Data = new byte[0];
        }

        protected override void ProcessRawReport()
        {
            TransmisionType = (DeviceTransmisionType)ReportReceived.UserData[TRANSMISSION_TYPE_INDEX];
            Data = new byte[MAX_DATA_LENGTH];
            Array.Copy(ReportReceived.UserData, DATA_INDEX, Data, 0, MAX_DATA_LENGTH);
        }
    }
}
