using System.Net.Sockets;

namespace PortForwarder
{
    internal class ClientPair
    {
        public readonly byte[] sourceBuffer = new byte[65536];
        public readonly byte[] targetBuffer = new byte[65536];
        public int connectRetryCount;
        public bool disconnected;
        public TcpClient source;
        public NetworkStream sourceStream;
        public TcpClient target;
        public NetworkStream targetStream;
    }
}