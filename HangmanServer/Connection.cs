using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace HangmanServer
{
    internal class Connection
    {
        private TcpListener listener;
        private int port;
        private IPAddress localAddr;
        private string serverEtag = Guid.NewGuid().ToString("N");

        public static string WebServerPath = "";

        private TcpClient? activeClient = null;
        private NetworkStream? activeStream = null;

        public Connection(string IP, int port)
        {
            this.port = port;
            this.localAddr = IPAddress.Parse(IP);

            listener = new TcpListener(localAddr, port);
            listener.Start();
        }

        public void Close()
        {
            CloseActiveClient();
            listener.Stop();
        }

        public void CloseActiveClient()
        {
            if (activeClient != null)
            {
                activeClient.Close();
            }
        }

        public string GetRequest()
        {
            if (!listener.Pending())
            {
                Thread.Sleep(500); 
                return "";
            }

            activeClient = AcceptClient();
            activeStream = activeClient.GetStream();

            string request;
            byte[] bytes = new byte[1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int numBytesRead;
                while ((numBytesRead = activeStream.Read(bytes, 0, bytes.Length)) > 0)
                {
                    ms.Write(bytes, 0, numBytesRead);
                    if (numBytesRead != 1024)
                        break;
                }
                request = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
            }

            return request;
        }

        public void SendHeaders(string? httpVersion, int statusCode, string statusMsg, string? contentType, string? contentEncoding,
            int byteLength)
        {
            string responseHeaderBuffer = "";

            responseHeaderBuffer = $"HTTP/1.1 {statusCode} {statusMsg}\r\n" +
                $"Connection: Keep-Alive\r\n" +
                $"Date: {DateTime.UtcNow.ToString()}\r\n" +
                $"Server: Windows PC \r\n" +
                $"Content-Encoding: {contentEncoding}\r\n" +
                "X-Content-Type-Options: nosniff" +
                $"Content-Type: application/signed-exchange;v=b3\r\n\r\n";

            byte[] responseBytes = Encoding.UTF8.GetBytes(responseHeaderBuffer);
            activeStream!.Write(responseBytes, 0, responseBytes.Length);
        }

        public void WriteStream(string data)
        {
            byte[] data_b = Encoding.ASCII.GetBytes(data);
            activeStream!.Write(data_b, 0, data_b.Length);
        }

        public void WriteStream(byte[] data)
        {
            activeStream!.Write(data, 0, data.Length);
        }

        public NetworkStream? GetActiveStream()
        {
            return activeStream;
        }

        private TcpClient AcceptClient()
        {
            return listener.AcceptTcpClient();
        }
    }
}
