using System;
using System.Windows.Forms;
using INFRA.USB.HidToSerial;

namespace INFRA.USB.Classes
{
    public delegate void ReportRecievedEventHandler(object sender, ReportRecievedEventArgs e);
    public delegate void SerialPacketRecievedEventHandler(object sender, SerialPacketRecievedEventArgs e);

    public class ReportRecievedEventArgs : EventArgs
    {
        public readonly HidInputReport Report;
        public ReportRecievedEventArgs(HidInputReport report)
        {
            Report = report;
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
