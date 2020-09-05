using System.IO;
using DnsChanger.Core.Models;
using Jil;

namespace DnsChanger.Core
{
    public static class ConfigHelper
    {
        public static Config Instance { get; private set; } = null;

        public static void Init(string configFile)
        {
            if (Instance != null)
            {
                return;
            }
            
            using var file = File.OpenRead(configFile);
            using var reader = new StreamReader(file);
            Instance = JSON.Deserialize<Config>(reader);
            
            GatewayHelper.Init(Instance);
        }
    }
}