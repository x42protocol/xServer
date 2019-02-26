using System;
using System.Net;

namespace X42.Feature.X42Client.Models.Event
{
    public class ConnectDisconnectEvent : BaseEvent
    {
        public readonly IPAddress Address;
        public readonly bool IsConnected;
        public readonly ushort Port;


        public ConnectDisconnectEvent(bool isConnected, IPAddress address, ushort port)
        {
            IsConnected = isConnected;
            Address = address;
            Port = port;
            Time = DateTime.Now;
        }
    } //end of public class ConnectDisconnectEvent: EventArgs
}