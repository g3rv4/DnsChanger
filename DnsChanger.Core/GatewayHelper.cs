using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DnsChanger.Core.Models;
using Renci.SshNet;

namespace DnsChanger.Core
{
    public static class GatewayHelper
    {
        private static ConnectionInfo _connectionInfo = null;
        private static string _dnsToOverride = null;

        public static void Init(Config config)
        {
            _connectionInfo = new ConnectionInfo(config.Gateway.Ip, config.Gateway.Username, new PasswordAuthenticationMethod(config.Gateway.Username, config.Gateway.Password));
            _dnsToOverride = config.DNSToOverride;
        }

        private static Regex _rulesParsingRegex = new Regex("^([0-9]+) .* DnsChanger ([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+) .*$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex _wanIpParsingRegex = new Regex("([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+)", RegexOptions.Compiled);

        private static string ExecuteCommand(string command)
        {
            using var client = new SshClient(_connectionInfo);
            client.Connect();
            
            var cmd = client.CreateCommand(command);
            cmd.Execute();
            return cmd.Result;
        }
        
        public static ImmutableDictionary<string, int> GetRedirectedIps()
        {
            var output = ExecuteCommand("sudo /sbin/iptables -L PREROUTING --line-numbers -t nat | grep DnsChanger");
            
            var res = ImmutableDictionary.CreateBuilder<string, int>();
            foreach (var line in output.Split('\n'))
            {
                var match = _rulesParsingRegex.Match(line);
                if (match.Success)
                {
                    res.Add(match.Groups[2].Value, int.Parse(match.Groups[1].Value));
                }
            }

            return res.ToImmutable();
        }

        public static bool AddRedirection(string ip)
        {
            if (IpIsRedirected(ip))
            {
                return true;
            }

            ExecuteCommand($"sudo /sbin/iptables -t nat -A PREROUTING -s {ip} -p udp --dport 53 -j DNAT --to-destination {_dnsToOverride}:53 -m comment --comment \"DnsChanger {ip} \"");
            return IpIsRedirected(ip);
        }

        public static bool DeleteRedirection(string ip)
        {
            var status = GetRedirectedIps();
            if (!status.ContainsKey(ip))
            {
                return true;
            }

            ExecuteCommand($"sudo /sbin/iptables -D PREROUTING {status[ip]} -t nat");
            return !IpIsRedirected(ip);
        }

        public static string GetCurrentIp()
        {
            var response = ExecuteCommand("/opt/vyatta/bin/vyatta-op-cmd-wrapper show interfaces | grep pppoe");
            var match = _wanIpParsingRegex.Match(response);
            return match.Success ? match.Groups[1].Value : null;
        }

        public static void ChangeWanIp() =>
            ExecuteCommand("/opt/vyatta/bin/vyatta-op-cmd-wrapper disconnect interface pppoe0;/opt/vyatta/bin/vyatta-op-cmd-wrapper connect interface pppoe0");

        private static bool IpIsRedirected(string ip) => GetRedirectedIps().ContainsKey(ip);
    }
}
