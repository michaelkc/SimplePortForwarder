namespace PortForwarder
{
    internal class PortForwarderOptions
    {
        public int LocalPort { get; set; }
        public string RemoteHost { get; set; } = string.Empty;
        public int RemotePort { get; set; }
    }
}
