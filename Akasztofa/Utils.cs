using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akasztofa
{
    internal class Utils
    {        
        public static string GetPassword()
        {
            string pass = "";
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            return pass;
        }

        public static (string ip, int port) ReconfigureEndpoint()
        {
            int port = 6969;
            string ip = "localhost";

            bool valid_input = false;
            do
            {
                Console.WriteLine("Enter a valid IP address and port (format ___.___.___.___:____)");
                string line = Console.ReadLine()!;
                string[] ip_port = line.Trim().Split(":");
                if (ip_port.Length == 2)
                {
                    if (int.TryParse(ip_port[1], out port))
                    {
                        if(port >= 1000 && port <= 9999)
                        {
                            if (ip_port[0] == "localhost")
                            {
                                ip = "localhost";
                            }
                            else
                            {
                                string[] ip_parts = ip_port[0].Trim().Split(".");
                                if (ip_parts.Length == 4)
                                {
                                    bool good = true;
                                    foreach(string part in ip_parts)
                                    {
                                        int part_as_int;
                                        if(int.TryParse(part, out part_as_int))
                                        {
                                            if(part_as_int < 0 || part_as_int > 255)
                                            {
                                                good = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            good = false;
                                            break;
                                        }
                                    }

                                    if(good)
                                    {
                                        ip = ip_port[0];
                                        valid_input = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            while (!valid_input);

            return (ip, port);
        }
    }
}
