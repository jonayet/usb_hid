using System;

namespace INFRA.USB.Classes
{
    public delegate void DataSentEventHandler(object sender, DataSentEventArgs e);
    public delegate void DataRecievedEventHandler(object sender, DataRecievedEventArgs e);
    public delegate void SerialPacketRecievedEventHandler(object sender, SerialPacketRecievedEventArgs e);

    public class DataSentEventArgs : EventArgs
    {
        public readonly byte[] data;
        public DataSentEventArgs(byte[] data)
        {
            this.data = data;
        }
    }

    public class DataRecievedEventArgs : EventArgs
    {
        public readonly byte[] Data;
        public DataRecievedEventArgs(byte[] data)
        {
            Data = data;
        }
    }

    public class SerialPacketRecievedEventArgs : EventArgs
    {
        public readonly byte[] RawData;
        public readonly object PacketData;
        public readonly DeviceTransmisionType DeviceTransmisionType;
        public SerialPacketRecievedEventArgs(byte[] rawData, DeviceTransmisionType transmisionType, object packetData)
        {
            RawData = rawData;
            PacketData = packetData;
            DeviceTransmisionType = transmisionType;
        }
    }
}
