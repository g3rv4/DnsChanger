using System;
using DnsChanger.Core;

namespace DnsChanger.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigHelper.Init("/Users/gervasio/Projects/DnsChanger/config.json");
            
            var status = GatewayHelper.GetRedirectedIps();
            GatewayHelper.AddRedirection("192.168.4.6");
            GatewayHelper.AddRedirection("192.168.3.9");
            status = GatewayHelper.GetRedirectedIps();
            GatewayHelper.DeleteRedirection("192.168.4.6");
            GatewayHelper.DeleteRedirection("192.168.3.9");
            status = GatewayHelper.GetRedirectedIps(); 
            Console.WriteLine("Hello World!");
        }
    }
}
