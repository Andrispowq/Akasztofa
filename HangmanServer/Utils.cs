﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akasztofa
{
    internal class Utils
    {
        public static (Dictionary<string, string> headers, string requestType) ParseHeaders(string headerString)
        {
            var headerLines = headerString.Split('\r', '\n');
            string firstLine = headerLines[0];
            var headerValues = new Dictionary<string, string>();

            foreach (var headerLine in headerLines)
            {
                var headerDetail = headerLine.Trim();
                var delimiterIndex = headerLine.IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    var headerName = headerLine.Substring(0, delimiterIndex).Trim();
                    var headerValue = headerLine.Substring(delimiterIndex + 1).Trim();
                    headerValues.Add(headerName, headerValue);
                }
            }

            return (headerValues, firstLine);
        }

        public static string GenerateEncryptionKey()
        {
            string key_string = "";

            long currentTimeMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            int seed = (int)(currentTimeMillis % int.MaxValue);
            Random random = new Random(seed);
            byte[] key = new byte[128];
            random.NextBytes(key);
            for (int i = 0; i < key.Length; i++)
            {
                key_string += key[i].ToString("X2");
            }

            return key_string;
        }
    }
}
