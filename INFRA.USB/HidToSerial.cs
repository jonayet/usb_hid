using System;
/*
typedef union
{
    unsigned char TransmisionType;
    
    struct
    {
        unsigned char bytes[64];
    } Raw;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char BaudRateIndex;
    } BaudRateCommand_FromHost;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char ThisSegmentDataLength;
        unsigned char ExpectedDataLength;
        unsigned int Timeout;
        unsigned char DataArray[59];
    } SingleQuery_FromHost;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char ThisSegmentDataLength;
        unsigned int NoOfRemainingPackets;
        unsigned char LastPacketLength;
        unsigned char DeviceAckByte;
        unsigned char DataArray[58];
    } SyncOutData_FromHost;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned int Timeout;
    } SyncInStart_FromHost;
    
    struct
    {
        unsigned char TransmisionType;
    } SyncInRead_FromHost;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char HostAckByte;
    } SyncInAck_FromHost;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char ThisSegmentDataLength;
        unsigned char DataArray[62];
    } AsyncOut_FromHost;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char RequiredDataLength;
        unsigned int Timeout;
    } AsyncInStart_FromHost;
    
    struct
    {
        unsigned char TransmisionType;
    } AsyncInStop_FromHost;
} HostPacketData;

typedef union
{
    struct
    {
        unsigned char bytes[64];
    } Raw;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char DataArray[40];
    } BaudRateResponse_FromDevice;

    struct
    {
        unsigned char TransmisionType;
        unsigned char ThisSegmentDataLength;
        unsigned char DataArray[62];
    } SingleResponse_FromDevice;

    struct
    {
        unsigned char TransmisionType;
        unsigned char DeviceAckByte;
    } SyncOutAck_FromDevice;

    struct
    {
        unsigned char TransmisionType;
        unsigned char ThisSegmentDataLength;
        unsigned char NoOfRemainingPackets;
        unsigned char LastPacketLength;
        unsigned char HostAckByte;
        unsigned char DataArray[59];
    } SyncInData_FromDevice;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char ThisSegmentDataLength;
        unsigned char DataArray[62];
    } AsyncInData_FromDevice;
    
    struct
    {
        unsigned char TransmisionType;
        unsigned char DataArray[10];
    } UnknownResponse_FromDevice;
} DevicePacketData;

typedef enum
{
    NONE_FROM_HOST = 0,
    BAUDRATE_CMD_FROM_HOST,
    SINGLE_QUERY_FROM_HOST,
    SYNC_OUT_DATA_FROM_HOST,
    SYNC_IN_START_FROM_HOST,
    SYNC_IN_READ_FROM_HOST,
    SYNC_IN_ACK_FROM_HOST,
    ASYNC_OUT_FROM_HOST,
    ASYNC_IN_START_FROM_HOST,
    ASYNC_IN_STOP_FROM_HOST,
    UNKNOWN_FROM_HOST
} HostTransmisionType;

typedef enum
{
    NONE_FROM_DEVICE = 0,
    BAUDRATE_RESP_FROM_DEVICE,
    SINGLE_RESPONSE_FROM_DEVICE,
    SYNC_OUT_ACK_FROM_DEVICE,
    SYNC_IN_DATA_FROM_DEVICE,
    ASYNC_IN_DATA_FROM_DEVICE,
    UNKNOWN_FROM_DEVICE
} DeviceTransmisionType;
*/
using System.Diagnostics;

namespace INFRA.USB
{
    public enum HostTransmisionType
    {
        NONE_FROM_HOST = 0,
        BAUDRATE_CMD_FROM_HOST,
        SINGLE_QUERY_FROM_HOST,
        SYNC_OUT_DATA_FROM_HOST,
        SYNC_IN_START_FROM_HOST,
        SYNC_IN_READ_FROM_HOST,
        SYNC_IN_ACK_FROM_HOST,
        ASYNC_OUT_FROM_HOST,
        ASYNC_IN_START_FROM_HOST,
        ASYNC_IN_STOP_FROM_HOST,
        UNKNOWN_FROM_HOST
    }

    public enum DeviceTransmisionType
    {
        NONE_FROM_DEVICE = 0,
        BAUDRATE_RESP_FROM_DEVICE,
        SINGLE_RESPONSE_FROM_DEVICE,
        SYNC_OUT_ACK_FROM_DEVICE,
        SYNC_IN_DATA_FROM_DEVICE,
        ASYNC_IN_DATA_FROM_DEVICE,
        UNKNOWN_FROM_DEVICE
    }

    public class Seri
    {
        #region Packet Communication Methods
        /*
         /// <summary>
        /// Contains last TransmisionType from Host
        /// </summary>
        public HostTransmisionType HostTransmisionType { get { return _hostTransmisionType; } }

        /// <summary>
        /// Contains last TransmisionType from Device
        /// </summary>
        public DeviceTransmisionType DeviceTransmisionType { get { return _deviceTransmisionType; } }

        /// <summary>
        /// Contains last SegmentLength from Device
        /// </summary>
        public int DeviceSegmentLength { get { return _deviceSegmentLength; } }

        /// <summary>
        /// Contains last AckByte from Device
        /// </summary>
        public int DeviceAckByte { get { return _deviceAckByte; } }

        /// <summary>
        /// Contains last HostAckByte from Device
        /// </summary>
        public int HostAckByte { get { return _hostAckByte; } }

        /// <summary>
        /// represents last packet response is not received yet
        /// </summary>
        public bool IsPacketReceiving { get { return _isPacketReceiving; } }

        public bool HasResponseFromDevice { get { return HasResponse(); } }

        public object ReceivedPacket { get; private set; }

        private HostTransmisionType _hostTransmisionType = HostTransmisionType.NONE_FROM_HOST;
        private DeviceTransmisionType _deviceTransmisionType = DeviceTransmisionType.NONE_FROM_DEVICE;
        private int _deviceSegmentLength = 0;
        private int _deviceAckByte = 0;
        private int _hostAckByte = 0;
        private bool _isPacketReceiving = false;

        private void ProcessPacket(byte[] inputReport)
        {
            var transmisionType = (DeviceTransmisionType)inputReport[1];
            _deviceTransmisionType = transmisionType;
            object packetData = null;

            switch (transmisionType)
            {
                case DeviceTransmisionType.BAUDRATE_RESP_FROM_DEVICE:
                    packetData = new BaudRateResponse_FromDevice(inputReport);
                    break;
                case DeviceTransmisionType.SINGLE_RESPONSE_FROM_DEVICE:
                    packetData = new SingleResponse_FromDevice(inputReport);
                    _deviceSegmentLength = (packetData as SingleResponse_FromDevice).ThisSegmentDataLength;
                    break;
                case DeviceTransmisionType.SYNC_OUT_ACK_FROM_DEVICE:
                    packetData = new SyncOutAck_FromDevice(inputReport);
                    _deviceAckByte = (packetData as SyncOutAck_FromDevice).DeviceAckByte;
                    break;
                case DeviceTransmisionType.SYNC_IN_DATA_FROM_DEVICE:
                    packetData = new SyncInData_FromDevice(inputReport);
                    _deviceSegmentLength = (packetData as SyncInData_FromDevice).ThisSegmentDataLength;
                    _hostAckByte = (packetData as SyncInData_FromDevice).HostAckByte;
                    break;
                case DeviceTransmisionType.ASYNC_IN_DATA_FROM_DEVICE:
                    packetData = new AsyncInData_FromDevice(inputReport);
                    _deviceSegmentLength = (packetData as AsyncInData_FromDevice).ThisSegmentDataLength;
                    break;
                case DeviceTransmisionType.UNKNOWN_FROM_DEVICE:
                    packetData = new UnknownResponse_FromDevice(inputReport);
                    break;
                default:
                    transmisionType = DeviceTransmisionType.NONE_FROM_DEVICE;
                    packetData = inputReport;
                    break;
            }

            ReceivedPacket = packetData;
            _isPacketReceiving = false;

            // Fire the event handler if assigned
            SerialPacketRecievedEventHandler handler;
            lock (DataReceived) { handler = SerialPacketRecieved; }
            if (handler != null)
            {
                handler(this, new SerialPacketRecievedEventArgs(inputReport, transmisionType, packetData));
            }
        }

        private void SendPacketData(byte[] _dataBytes)
        {
            SendData(_dataBytes);
            _isPacketReceiving = HasResponse();
            if (!_isPacketReceiving)
                System.Threading.Thread.Sleep(50);
        }

        private bool HasResponse()
        {
            switch (HostTransmisionType)
            {
                case HostTransmisionType.BAUDRATE_CMD_FROM_HOST: return true;
                case HostTransmisionType.SINGLE_QUERY_FROM_HOST: return true;
                case HostTransmisionType.SYNC_OUT_DATA_FROM_HOST: return true;
                case HostTransmisionType.SYNC_IN_START_FROM_HOST: return true;
                case HostTransmisionType.SYNC_IN_READ_FROM_HOST: return true;
                case HostTransmisionType.ASYNC_IN_START_FROM_HOST: return true;
                default: return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baudRateIndex">if 1, Baudrate : 1200; if 2, Baudrate : 2400; if 4, Baudrate : 4800; if 9, Baudrate : 9600; if 14, Baudrate : 14400; if 19, Baudrate : 19200; if 38, Baudrate : 38400; if 56, Baudrate : 56000; if 57, Baudrate : 57600; if 115, Baudrate : 115200; if 128, Baudrate : 128000;</param>
        public void SendBaudRatePacket(int baudRateIndex)
        {
            BaudRateCommand_FromHost baudRatePacket = new BaudRateCommand_FromHost();
            baudRatePacket.BaudRateIndex = baudRateIndex;
            _hostTransmisionType = baudRatePacket.HostTransmisionType;
            SendPacketData(baudRatePacket.RawBytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataBytes">Max Length: 59</param>
        /// <param name="expectedDataLength">Max: 62</param>
        /// <param name="timeout">Max: 65535</param>
        public void SendSingleQueryPacket(byte[] dataBytes, int expectedDataLength, int timeout)
        {
            SingleQuery_FromHost singleQueryPacket = new SingleQuery_FromHost();
            singleQueryPacket.ThisSegmentDataLength = dataBytes.Length;
            singleQueryPacket.ExpectedDataLength = expectedDataLength;
            singleQueryPacket.Timeout = timeout;
            singleQueryPacket.SetData(dataBytes, 0, singleQueryPacket.ThisSegmentDataLength);
            _hostTransmisionType = singleQueryPacket.HostTransmisionType;
            SendPacketData(singleQueryPacket.RawBytes);
        }

        bool isSyncPacketPending = false;
        private int dataAddressOffset = 0;
        public bool SendSyncOutPackets(byte[] dataBytes, bool startFromBeginning, out int thisSegmentDataLength, out int noOfRemainingPackets, out int lastSegmentLength)
        {
            SyncOutData_FromHost syncOutPacket = new SyncOutData_FromHost();

            // start sending from the beginning
            if (startFromBeginning)
            {
                isSyncPacketPending = false;
                dataAddressOffset = 0;
            }

            if (!isSyncPacketPending)
            {
                // 1st time in sync out operation
                // set isSyncPacketSending, so that all packets will send sequentally if data length > max segment data capacity
                if (dataBytes.Length <= syncOutPacket.DataArray.Length)
                {
                    // data length <= max segment data capacity
                    // send single packet
                    syncOutPacket.ThisSegmentDataLength = dataBytes.Length;
                    syncOutPacket.NoOfRemainingPackets = 0;
                    syncOutPacket.LastPacketLength = 0;
                    syncOutPacket.DeviceAckByte = 0x1F;
                    syncOutPacket.SetData(dataBytes, 0, syncOutPacket.ThisSegmentDataLength);
                    isSyncPacketPending = false;
                }
                else
                {
                    // data length > max segment data capacity
                    // send 1st packet
                    syncOutPacket.ThisSegmentDataLength = syncOutPacket.DataArray.Length;
                    syncOutPacket.NoOfRemainingPackets = dataBytes.Length / syncOutPacket.DataArray.Length;
                    syncOutPacket.LastPacketLength = dataBytes.Length % syncOutPacket.DataArray.Length;
                    syncOutPacket.DeviceAckByte = 0x1C;
                    syncOutPacket.SetData(dataBytes, 0, syncOutPacket.ThisSegmentDataLength);
                    dataAddressOffset = 0;
                    isSyncPacketPending = true;
                }
            }
            else
            {
                dataAddressOffset += syncOutPacket.DataArray.Length;
                if (dataBytes.Length > (dataAddressOffset + syncOutPacket.DataArray.Length))
                {
                    // send all packets will send sequentally (starts from 2nd packet)
                    syncOutPacket.ThisSegmentDataLength = syncOutPacket.DataArray.Length;
                    syncOutPacket.NoOfRemainingPackets = (dataBytes.Length - dataAddressOffset) / syncOutPacket.DataArray.Length;
                    syncOutPacket.LastPacketLength = dataBytes.Length % syncOutPacket.DataArray.Length;
                    syncOutPacket.DeviceAckByte = 0x2C;
                    syncOutPacket.SetData(dataBytes, dataAddressOffset, syncOutPacket.ThisSegmentDataLength);
                }
                else
                {
                    // send last packet
                    syncOutPacket.ThisSegmentDataLength = dataBytes.Length % syncOutPacket.DataArray.Length;
                    syncOutPacket.NoOfRemainingPackets = 0;
                    syncOutPacket.LastPacketLength = 0;
                    syncOutPacket.DeviceAckByte = 0x2F;
                    syncOutPacket.SetData(dataBytes, dataAddressOffset, syncOutPacket.ThisSegmentDataLength);
                    isSyncPacketPending = false;
                }
            }

            // update parameters
            thisSegmentDataLength = syncOutPacket.ThisSegmentDataLength;
            noOfRemainingPackets = syncOutPacket.NoOfRemainingPackets;
            lastSegmentLength = syncOutPacket.LastPacketLength;

            // send data through hid bus
            _hostTransmisionType = syncOutPacket.HostTransmisionType;
            SendPacketData(syncOutPacket.RawBytes);
            return isSyncPacketPending;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout">Max: 65535</param>
        public void SendSyncInStartPacket(int timeout)
        {
            SyncInStart_FromHost syncInStartPacket = new SyncInStart_FromHost();
            syncInStartPacket.Timeout = timeout;
            _hostTransmisionType = syncInStartPacket.HostTransmisionType;
            SendPacketData(syncInStartPacket.RawBytes);
        }

        public void SendSyncInReadPacket()
        {
            SyncInRead_FromHost syncInReadPacket = new SyncInRead_FromHost();
            _hostTransmisionType = syncInReadPacket.HostTransmisionType;
            SendPacketData(syncInReadPacket.RawBytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ackByte">8 bit</param>
        public void SendSyncInAckPacket(int ackByte)
        {
            SyncInAck_FromHost syncInAckPacket = new SyncInAck_FromHost();
            syncInAckPacket.HostAckByte = ackByte;
            _hostTransmisionType = syncInAckPacket.HostTransmisionType;
            SendPacketData(syncInAckPacket.RawBytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataBytes">Max Length: 62</param>
        public void SendAsyncOutPacket(byte[] dataBytes)
        {
            AsyncOut_FromHost asyncOutPacket = new AsyncOut_FromHost();
            asyncOutPacket.ThisSegmentDataLength = dataBytes.Length;
            asyncOutPacket.SetData(dataBytes, 0, asyncOutPacket.ThisSegmentDataLength);
            _hostTransmisionType = asyncOutPacket.HostTransmisionType;
            SendPacketData(asyncOutPacket.RawBytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requiredDataLength">Max: 62. If 0, device will wait until timout occured.</param>
        /// <param name="timeout">Max: 65535. Should be > 0</param>
        public void SendAsyncInStartPacket(int requiredDataLength, int timeout)
        {
            AsyncInStart_FromHost asyncInStartPacket = new AsyncInStart_FromHost();
            asyncInStartPacket.RequiredDataLength = requiredDataLength;
            asyncInStartPacket.Timeout = timeout;
            _hostTransmisionType = asyncInStartPacket.HostTransmisionType;
            SendPacketData(asyncInStartPacket.RawBytes);
        }

        public void SendAsyncInStopPacket()
        {
            AsyncInStop_FromHost asyncInStopPacket = new AsyncInStop_FromHost();
            _hostTransmisionType = asyncInStopPacket.HostTransmisionType;
            SendPacketData(asyncInStopPacket.RawBytes);
        }

        public void SendData(byte[] data)
        {
            try
            {
                Write(data); // write the output report
            }
            catch (HIDDeviceException ex1)
            {
                // Device may have been removed!
                Debug.WriteLine(ex1.ToString());
            }
            catch (Exception ex2)
            {
                Debug.WriteLine(ex2.ToString());
            }
        }
        */
        #endregion
    }

    public class BaudRateCommand_FromHost
    {
        private byte[] _rawBytes;           // 65 byte

        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.BAUDRATE_CMD_FROM_HOST;
            }
        }

        /// <summary>
        /// index=1 > Baudrate : 1200; Index=2 > Baudrate : 2400; index=4 > Baudrate : 4800;
        /// index=9 > Baudrate : 9600; Index=14 > Baudrate : 14400; index=19 > Baudrate : 19200;
        /// index=38 > Baudrate : 38400; index=56 > Baudrate : 56000; index=57 > Baudrate : 57600;
        /// Index=115 > Baudrate : 115200; index=128 > Baudrate : 128000;
        /// </summary>
        public int BaudRateIndex { get; set; }

        public BaudRateCommand_FromHost()
        {
            BaudRateIndex = 0;
            _rawBytes = new byte[65];
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index++] = (byte)HostTransmisionType;
            _rawBytes[index] = (byte)BaudRateIndex;
        }
    }

    public class SingleQuery_FromHost
    {
        private byte[] _rawBytes;
        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.SINGLE_QUERY_FROM_HOST;
            }
        }

        /// <summary>
        /// Max: 59
        /// </summary>
        public int ThisSegmentDataLength { get; set; }

        /// <summary>
        /// Max: 62
        /// </summary>
        public int ExpectedDataLength { get; set; }

        /// <summary>
        /// Max: 65535
        /// </summary>
        public int Timeout { get; set; }

        public byte[] DataArray  { get; private set; }

        public SingleQuery_FromHost()
        {
            _rawBytes = new byte[65];
            DataArray = new byte[59];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">Max: 59</param>
        public void SetData(byte[] source, int startIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                DataArray[i] = source[startIndex + i];
            }
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index++] = (byte)HostTransmisionType;
            _rawBytes[index++] = (byte)ThisSegmentDataLength;
            _rawBytes[index++] = (byte)ExpectedDataLength;
            _rawBytes[index++] = BitConverter.GetBytes(Timeout)[0];
            _rawBytes[index++] = BitConverter.GetBytes(Timeout)[1];
            for (int i = 0; i < DataArray.Length; i++)
            {
                _rawBytes[index++] = DataArray[i];
            }
        }
    }

    public class SyncOutData_FromHost
    {
        private byte[] _rawBytes;
        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.SYNC_OUT_DATA_FROM_HOST;
            }
        }

        /// <summary>
        /// Max: 58
        /// </summary>
        public int ThisSegmentDataLength { get; set; }

        /// <summary>
        /// Max: 65535
        /// </summary>
        public int NoOfRemainingPackets { get; set; }

        /// <summary>
        /// Max: 58
        /// </summary>
        public int LastPacketLength { get; set; }

        /// <summary>
        /// 8 bit
        /// </summary>
        public int DeviceAckByte { get; set; }

        public byte[] DataArray  { get; private set; }

        public SyncOutData_FromHost()
        {
            _rawBytes = new byte[65];
            DataArray = new byte[58];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">Max: 58</param>
        public void SetData(byte[] source, int startIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                DataArray[i] = source[startIndex + i];
            }
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index++] = (byte)HostTransmisionType;
            _rawBytes[index++] = (byte)ThisSegmentDataLength;
            _rawBytes[index++] = BitConverter.GetBytes(NoOfRemainingPackets)[0];
            _rawBytes[index++] = BitConverter.GetBytes(NoOfRemainingPackets)[1];
            _rawBytes[index++] = (byte)LastPacketLength;
            _rawBytes[index++] = (byte)DeviceAckByte;
            for (int i = 0; i < DataArray.Length; i++)
            {
                _rawBytes[index++] = DataArray[i];
            }
        }
    }

    public class SyncInStart_FromHost
    {
        private byte[] _rawBytes;
        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.SYNC_IN_START_FROM_HOST;
            }
        }

        /// <summary>
        /// Max: 65535
        /// </summary>
        public int Timeout { get; set; }

        public SyncInStart_FromHost()
        {
            _rawBytes = new byte[65];
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index++] = (byte)HostTransmisionType;
            _rawBytes[index++] = BitConverter.GetBytes(Timeout)[0];
            _rawBytes[index] = BitConverter.GetBytes(Timeout)[1];
        }
    }

    public class SyncInRead_FromHost
    {
        private byte[] _rawBytes;
        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.SYNC_IN_READ_FROM_HOST;
            }
        }

        public SyncInRead_FromHost()
        {
            _rawBytes = new byte[65];
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index++] = (byte)HostTransmisionType;
        }
    }

    public class SyncInAck_FromHost
    {
        private byte[] _rawBytes;
        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.SYNC_IN_ACK_FROM_HOST;
            }
        }

        /// <summary>
        /// 8 bit
        /// </summary>
        public int HostAckByte { get; set; }

        public SyncInAck_FromHost()
        {
            _rawBytes = new byte[65];
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index++] = (byte)HostTransmisionType;
            _rawBytes[index] = (byte)HostAckByte;
        }
    }

    public class AsyncOut_FromHost
    {
        private byte[] _rawBytes;
        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.ASYNC_OUT_FROM_HOST;
            }
        }

        /// <summary>
        /// Max: 62
        /// </summary>
        public int ThisSegmentDataLength { get; set; }

        public byte[] DataArray { get; private set; }

        public AsyncOut_FromHost()
        {
            _rawBytes = new byte[65];
            DataArray = new byte[62];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length">Max: 62</param>
        public void SetData(byte[] source, int startIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                DataArray[i] = source[startIndex + i];
            }
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index++] = (byte)HostTransmisionType;
            _rawBytes[index++] = (byte)ThisSegmentDataLength;
            for (int i = 0; i < DataArray.Length; i++)
            {
                _rawBytes[index++] = DataArray[i];
            }
        }
    }

    public class AsyncInStart_FromHost
    {
        private byte[] _rawBytes;
        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.ASYNC_IN_START_FROM_HOST;
            }
        }

        /// <summary>
        /// Max: 62
        /// </summary>
        public int RequiredDataLength { get; set; }

        /// <summary>
        /// Max: 65535
        /// </summary>
        public int Timeout { get; set; }

        public AsyncInStart_FromHost()
        {
            _rawBytes = new byte[65];
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index++] = (byte)HostTransmisionType;
            _rawBytes[index++] = (byte)RequiredDataLength;
            _rawBytes[index++] = BitConverter.GetBytes(Timeout)[0];
            _rawBytes[index] = BitConverter.GetBytes(Timeout)[1];
        }
    }

    public class AsyncInStop_FromHost
    {
        private byte[] _rawBytes;
        public byte[] RawBytes
        {
            get
            {
                SetRawData();
                return _rawBytes;
            }
        }

        public HostTransmisionType HostTransmisionType
        {
            get
            {
                return HostTransmisionType.ASYNC_IN_STOP_FROM_HOST;
            }
        }

        public AsyncInStop_FromHost()
        {
            _rawBytes = new byte[65];
        }

        private void SetRawData()
        {
            int index = 1;  // hid start index start from 1
            _rawBytes[index] = (byte)HostTransmisionType;
        }
    }

    public class BaudRateResponse_FromDevice
    {
        private byte[] _rawBytes;
        private DeviceTransmisionType _transmissionType;
        private byte[] _dataBytes;

        public DeviceTransmisionType DeviceTransmisionType { get { return _transmissionType; } }
        public byte[] DataBytes { get { return _dataBytes; } }

        public BaudRateResponse_FromDevice(byte[] rawData)
        {
            _rawBytes = new byte[64];
            _dataBytes = new byte[40];

            // copy raw bytes
            HidToSerialCommon.CopyDataArray(ref _rawBytes, rawData, 1, _rawBytes.Length);

            // get transmission type
            _transmissionType = (DeviceTransmisionType)_rawBytes[0];

            // copy data bytes
            HidToSerialCommon.CopyDataArray(ref _dataBytes, _rawBytes, 1, DataBytes.Length);
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

    static class HidToSerialCommon
    {
        public static void CopyDataArray( ref byte[] destination, byte[] source, int startIndex, int length)
        {
            for(int i = 0; i < length; i++)
            {
                destination[i] = source[startIndex + i];
            }
        }
    }
}
