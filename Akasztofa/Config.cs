using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Akasztofa
{
    internal class Config
    {
        public struct ConfigData
        {
            public string serverIP { get; set; }
            public int serverPort { get; set; }
            public string clientID { get; set; }
        }

        private static string DefaultServerIP = "192.168.100.20";
        private static int DefaultServerPort = 6969;

        public static ConfigData LoadConfigData(string configFile)
        {
            ConfigData data;

            if(File.Exists(configFile))
            { 
                string json = File.ReadAllText(configFile);
                data = JsonSerializer.Deserialize<ConfigData>(json);
            }
            else
            {
                data = new ConfigData();
                data.serverIP = DefaultServerIP;
                data.serverPort = DefaultServerPort;
                data.clientID = Guid.NewGuid().ToString();

                string json = JsonSerializer.Serialize(data);
                File.WriteAllText(configFile, json);
            }

            return data;
        }
    }
}
