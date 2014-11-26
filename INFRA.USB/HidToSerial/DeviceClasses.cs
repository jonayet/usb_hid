using System;
using INFRA.USB.HelperClasses;

namespace INFRA.USB.HidToSerial
{
    public abstract class DevicePacket
    {
        public HidInputReport ReportReceived
        {
            protected set { _rawReport = value; ProcessRawReport(); }
            get { return _rawReport; }
        }
        public DeviceTransmisionType TransmisionType { get; protected set; }
        public byte[] DataReceived { get; protected set; }
        public int ThisSegmentDataLength { get; internal set; }
        public int NoOfRemainingPackets { get; protected set; }
        public int LastPacketLength { get; protected set; }
        public int DeviceAckByte { get; protected set; }
        public int HostAckByte { get; protected set; }

        private HidInputReport _rawReport;
        public DevicePacket()
        {
            _rawReport = new HidInputReport();
            DataReceived = new byte[0];
            ThisSegmentDataLength = 0;
            NoOfRemainingPackets = 0;
            LastPacketLength = 0;
            DeviceAckByte = 0;
            HostAckByte = 0;
        }

        protected abstract void ProcessRawReport();
    }

    public class BaudRateResponse_FromDevice : DevicePacket
    {
        public BaudRateResponse_FromDevice()
        {
            TransmisionType = DeviceTransmisionType.BAUDRATE_RESP_FROM_DEVICE;
            ThisSegmentDataLength = 40;
        }

        public BaudRateResponse_FromDevice(byte[] rawData)
        {
            //_rawBytes = new byte[64];
            //_dataBytes = new byte[40];

            //// copy raw bytes
            //HidToSerialCommon.CopyDataArray(ref _rawBytes, rawData, 1, _rawBytes.Length);

            //// get transmission type
            //_transmissionType = (DeviceTransmisionType)_rawBytes[0];

            //// copy data bytes
            //HidToSerialCommon.CopyDataArray(ref _dataBytes, _rawBytes, 1, DataBytes.Length);
        }

        protected override void ProcessRawReport()
        {
            Array.Copy(ReportReceived.UserData, DataReceived, ThisSegmentDataLength);
        }
    }

    public class SingleResponse_FromDevice
    {
        private byte[] _rawBytes;
        private DeviceTransmisionType _transmissionType;
        private byte[] _dataBytes;

        public DeviceTransmisionType DeviceTransmisionType { get { return _transmissionType; } }

        /// <summary>
        /// Max: 62
        /// </summary>
        public int ThisSegmentDataLength { get; private set; }
        public byte[] DataBytes { get { return _dataBytes; } }

        public SingleResponse_FromDevice(byte[] rawData)
        {
            _rawBytes = new byte[64];

            // copy raw bytes
            HidToSerialCommon.CopyDataArray(ref _rawBytes, rawData, 1, _rawBytes.Length);

            // get transmission type
            _transmissionType = (DeviceTransmisionType)_rawBytes[0];

            // get data length
            ThisSegmentDataLength = _rawBytes[1];

            // copy data bytes
            _dataBytes = new byte[ThisSegmentDataLength];
            HidToSerialCommon.CopyDataArray(ref _dataBytes, _rawBytes, 2, _dataBytes.Length);
        }
    }

    public class SyncOutAck_FromDevice
    {
        private byte[] _rawBytes;
        private DeviceTransmisionType _transmissionType;

        public DeviceTransmisionType DeviceTransmisionType { get { return _transmissionType; } }

        /// <summary>
        /// 8 bit
        /// </summary>
        public int DeviceAckByte { get; private set; }

        public SyncOutAck_FromDevice(byte[] rawData)
        {
            _rawBytes = new byte[64];

            // copy raw bytes
            HidToSerialCommon.CopyDataArray(ref _rawBytes, rawData, 1, _rawBytes.Length);

            // get transmission type
            _transmissionType = (DeviceTransmisionType)_rawBytes[0];

            // get device ack
            DeviceAckByte = _rawBytes[1];
        }
    }

    public class SyncInData_FromDevice
    {
        private byte[] _rawBytes;
        private DeviceTransmisionType _transmissionType;
        private byte[] _dataBytes;

        public DeviceTransmisionType DeviceTransmisionType { get { return _transmissionType; } }

        /// <summary>
        /// Max: 59
        /// </summary>
        public int ThisSegmentDataLength { get; private set; }

        /// <summary>
        /// Max: 15
        /// </summary>
        public int NoOfRemainingPackets { get; private set; }

        /// <summary>
        /// Max: 59
        /// </summary>
        public int LastPacketLength { get; private set; }

        /// <summary>
        /// 8 bit
        /// </summary>
        public int HostAckByte { get; private set; }
        public byte[] DataBytes { get { return _dataBytes; } }

        public SyncInData_FromDevice(byte[] rawData)
        {
            _rawBytes = new byte[64];

            // copy raw bytes
            HidToSerialCommon.CopyDataArray(ref _rawBytes, rawData, 1, _rawBytes.Length);

            // get transmission type
            _transmissionType = (DeviceTransmisionType)_rawBytes[0];

            // get data length
            ThisSegmentDataLength = _rawBytes[1];

            // get NoOfRemainingPackets
            NoOfRemainingPackets = _rawBytes[2];

            // get LastPacketLength
            LastPacketLength = _rawBytes[3];

            // get HostAckByte
            HostAckByte = _rawBytes[4];

            // copy data bytes
            _dataBytes = new byte[ThisSegmentDataLength];
            HidToSerialCommon.CopyDataArray(ref _dataBytes, _rawBytes, 5, _dataBytes.Length);
        }
    }

    public class AsyncInData_FromDevice
    {
        private byte[] _rawBytes;
        private DeviceTransmisionType _transmissionType;
        private byte[] _dataBytes;

        public DeviceTransmisionType DeviceTransmisionType { get { return _transmissionType; } }

        /// <summary>
        /// Max: 62
        /// </summary>
        public int ThisSegmentDataLength { get; private set; }
        public byte[] DataBytes { get { return _dataBytes; } }

        public AsyncInData_FromDevice(byte[] rawData)
        {
            _rawBytes = new byte[64];

            // copy raw bytes
            HidToSerialCommon.CopyDataArray(ref _rawBytes, rawData, 1, _rawBytes.Length);

            // get transmission type
            _transmissionType = (DeviceTransmisionType)_rawBytes[0];

            // get data length
            ThisSegmentDataLength = _rawBytes[1];

            // copy data bytes
            _dataBytes = new byte[ThisSegmentDataLength];
            HidToSerialCommon.CopyDataArray(ref _dataBytes, _rawBytes, 2, _dataBytes.Length);
        }
    }

    public class UnknownResponse_FromDevice
    {
        private byte[] _rawBytes;
        private DeviceTransmisionType _transmissionType;
        private byte[] _dataBytes;

        public DeviceTransmisionType DeviceTransmisionType { get { return _transmissionType; } }
        public byte[] DataBytes { get { return _dataBytes; } }

        public UnknownResponse_FromDevice(byte[] rawData)
        {
            _rawBytes = new byte[64];
            _dataBytes = new byte[10];

            // copy raw bytes
            HidToSerialCommon.CopyDataArray(ref _rawBytes, rawData, 1, _rawBytes.Length);

            // get transmission type
            _transmissionType = (DeviceTransmisionType)_rawBytes[0];

            // copy data bytes
            HidToSerialCommon.CopyDataArray(ref _dataBytes, _rawBytes, 1, _dataBytes.Length);
        }
    }
}
