using System;
using System.Net.Sockets;

namespace PortForwarder
{
    internal class BeforeTargetConnectArgs : EventArgs
    {
        public Socket client;

        public BeforeTargetConnectArgs(Socket client)
        {
            this.client = client;
        }
    }
}