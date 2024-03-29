using System.Collections.Immutable;
using DnsChanger.Core;
using DnsChanger.Core.Models;

namespace DnsChanger.Web.Models
{
    public class IndexModel
    {
        public string Ip { get; }
        
        public Config Config { get; }
        
        public ImmutableArray<DeviceWitStatus> Devices { get; }

        public IndexModel(ImmutableArray<DeviceWitStatus> devices, string ip)
        {
            Config = ConfigHelper.Instance;
            Devices = devices;
            Ip = ip;
        }
    }

    public class DeviceWitStatus
    {
        public Device Device { get; }
        public bool IsRedirected { get; }

        public DeviceWitStatus(Device device, bool isRedirected)
        {
            Device = device;
            IsRedirected = isRedirected;
        }
    }
}