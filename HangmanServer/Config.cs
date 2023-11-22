using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HangmanServer
{
    internal class Config
    {
        public struct ConfigData
        {
            public string serverIP { get; set; }
            public int serverPort { get; set; }
            public string serverID { get; set; }
            public string serverName { get; set; }
            public int timeoutMinutes { get; set; }
            public string serverFolder { get; set; }
        }

        private static string DefaultServerIP = "192.168.100.20";
        private static int DefaultServerPort = 6969;
        private static string DefaultServerName = "HangmanServer_v1.0";
        private static int DefaultTimeoutMinutes = 5;
        private static string DefaultServerFolder = "HangmanServerData";

        public ConfigData config;

        private Config() { }

        private static Config? instance = null;
        public static Config GetInstance()
        {
            if(instance == null)
            {
                instance = new Config();
            }

            return instance;
        }

        public static ConfigData LoadConfigData(string configFile)
        {
            ConfigData data;

            if (File.Exists(configFile))
            {
                string json = File.ReadAllText(configFile);
                data = JsonSerializer.Deserialize<ConfigData>(json);
            }
            else
            {
                data = new ConfigData();
                data.serverIP = DefaultServerIP;
                data.serverPort = DefaultServerPort;
                data.serverID = Guid.NewGuid().ToString();
                data.serverName = DefaultServerName;
                data.timeoutMinutes = DefaultTimeoutMinutes;
                data.serverFolder = DefaultServerFolder;

                string json = JsonSerializer.Serialize(data);
                File.WriteAllText(configFile, json);
            }

            return data;
        }
    }
}
