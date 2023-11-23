using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HangmanServer
{
    internal class Logger
    {
        private static FileStream stream;

        public static void InitialiseLogger()
        {
            string path = Connection.WebServerPath + "/logs";
            if(!FileSystem.DirectoryExists(path))
            {
                FileSystem.CreateDirectory(path);
            }

            var date = DateTime.Now;
            string timestamp = date.Year + "_" + date.Month + "_" + date.Day + "_" + date.Hour + "_" + date.Minute + "_" + date.Minute + "_" + date.Millisecond;
            string fileName = path + "/" + Config.GetInstance().config.serverName + "_" + timestamp + ".log";
            stream = File.Create(fileName);
            Log(0, "Initialised logger");
        }
        
        public static void ShutdownLogger()
        {
            stream.Close();
        }

        public static void Log(int severity, string message)
        {
            string level;
            switch(severity)
            {
                case 1:
                    level = "WARNING";
                    break;
                case 2:
                    level = "ERROR";
                    break;
                case 3:
                    level = "CRITICAL";
                    break;
                default:
                    level = "LOG";
                    break;
            }

            var date = DateTime.Now;
            string timestamp = date.Year + "_" + date.Month + "_" + date.Day + "_" + date.Hour + "_" + date.Minute + "_" + date.Minute + "_" + date.Millisecond;
            string msg = "[" + timestamp + "] - " + level + ": " + message + "\n";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);
        }
    }
}
