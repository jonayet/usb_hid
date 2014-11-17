using INFRA.USB.Classes;

namespace INFRA.USB.HidToSerial
{
    #region Transmission Types
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
    #endregion

    public class HidToserialCommunication
    {
        #region Packet Communication Methods
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
        private HidDevice _hidDevice;

        public HidToserialCommunication(HidDevice hidDevice)
        {
            _hidDevice = hidDevice;
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

        /*
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

    #region Host Commands
    
    #endregion

    

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
