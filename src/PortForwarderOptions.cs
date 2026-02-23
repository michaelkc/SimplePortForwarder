namespace PortForwarder
{
    internal class ForwardingRule
    {
        public int LocalPort { get; set; }
        public string RemoteHost { get; set; } = string.Empty;
        public int RemotePort { get; set; }
    }

    internal class PortForwarderOptions
    {
        public List<ForwardingRule> Rules { get; set; } = new();
    }
}
