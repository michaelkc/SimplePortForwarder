using System;
using System.Net.Sockets;

namespace PortForwarder
{
    internal class AfterTargetDisconnectArgs : EventArgs
    {
        public Socket client;

        public AfterTargetDisconnectArgs(Socket client)
        {
            this.client = client;
        }
    }
}