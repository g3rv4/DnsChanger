using System.Collections.Immutable;

namespace DnsChanger.Core.Models
{
    public class Config
    {
        public Gateway Gateway { get; private set; }
        public Device[] Devices { get; private set; }
        public string DNSToOverride { get; private set; }
    }

    public class Gateway
    {
        public string Ip { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
    }

    public class Device
    {
        public string Name { get; private set; }
        public string Ip { get; private set; }
    }
}