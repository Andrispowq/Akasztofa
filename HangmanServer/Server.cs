using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Akasztofa;
using System.Web;
using System.Runtime.InteropServices;
using System.Text.Json;
using static HangmanServer.Requests;

namespace HangmanServer
{
    internal class Server
    {
        private TcpListener listener;
        private int port;
        private IPAddress localAddr;
        private string serverEtag = Guid.NewGuid().ToString("N");

        public static string WebServerPath = @"WebServer";

        private Requests requests;

        public Server(string IP, int port)
        {
            this.port = port;
            this.localAddr = IPAddress.Parse(IP);

            requests = new Requests(WebServerPath + "/user_database.json");

            listener = new TcpListener(localAddr, port);
            listener.Start();
            Console.WriteLine($"Web Server Running on {localAddr.ToString()} on port {port}... Press ^C to Stop...");
            Thread th = new Thread(new ThreadStart(StartListen));
            th.Start();
        }

        private void StartListen()
        {
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                //read request 
                //byte[] requestBytes = new byte[1024];
                //int bytesRead = stream.Read(requestBytes, 0, requestBytes.Length);

                string request;
                byte[] bytes = new byte[1024];
                using (MemoryStream ms = new MemoryStream())
                {
                    int numBytesRead;
                    while ((numBytesRead = stream.Read(bytes, 0, bytes.Length)) > 0)
                    {
                        ms.Write(bytes, 0, numBytesRead);
                        if (numBytesRead != 1024)
                            break;
                    }
                    request = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                }

                //string request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
                var requestHeaders = ParseHeaders(request);

                string[] requestFirstLine = requestHeaders.requestType.Split(" ");
                string httpVersion = requestFirstLine.LastOrDefault();
                string contentType = requestHeaders.headers.GetValueOrDefault("Accept");
                string contentEncoding = requestHeaders.headers.GetValueOrDefault("Acept-Encoding");

                Console.WriteLine("New request: " + request);

                if (request.StartsWith("GET"))
                {
                    string req = requestFirstLine[1];
                    if(req.StartsWith("/?"))
                    {
                        req = req.Substring(2);
                    }

                    var parsed = HttpUtility.ParseQueryString(req);
                    string? type = parsed.Get("type");
                    string? username = parsed.Get("username");
                    string? password = parsed.Get("password");
                    string? data = parsed.Get("data");
                    if(type == "exists" && username != null)
                    {
                        //User exists
                        UserExistsRequest result = requests.HandleUserExists(username);

                        string result_s = JsonSerializer.Serialize(result);
                        byte[] result_a = Encoding.ASCII.GetBytes(result_s);
                        SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0, ref stream);
                        stream.Write(result_a, 0, result_a.Length);
                    }
                    else if(type == "login" && username != null && password != null)
                    {
                        //User login
                        UserLoginRequest result = requests.HandleUserLogin(username, password);

                        string result_s = JsonSerializer.Serialize(result);
                        byte[] result_a = Encoding.ASCII.GetBytes(result_s);
                        SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0, ref stream);
                        stream.Write(result_a, 0, result_a.Length);
                    }
                    else if(type == "create" &&  username != null && password != null)
                    {
                        //User creation
                        UserCreationRequest result = requests.HandleCreateUser(username, password);

                        string result_s = JsonSerializer.Serialize(result);
                        byte[] result_a = Encoding.ASCII.GetBytes(result_s);
                        SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0, ref stream);
                        stream.Write(result_a, 0, result_a.Length);
                    }
                    else if(type == "update" && username != null && password != null && data != null)
                    {
                        //User update
                        /*string new_data = "";

                        int BlockSize = 1024;
                        int DataRead = 0;
                        byte[] DataByte = new byte[BlockSize];
                        lock (this)
                        {
                            while (true)
                            {
                                DataRead = stream.Read(DataByte, 0, BlockSize);
                                new_data += Encoding.ASCII.GetString(DataByte);
                                if (DataRead == 0)
                                {
                                    break;
                                }
                            }
                        }*/

                        UserUpdateRequest result = requests.HandleUpdateUser(username, password, data);

                        string result_s = JsonSerializer.Serialize(result);
                        byte[] result_a = Encoding.ASCII.GetBytes(result_s);
                        SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0, ref stream);
                        stream.Write(result_a, 0, result_a.Length);
                    }
                    else if(type == "logout" && username != null && password != null)
                    {
                        //User logout
                        UserLogoutRequest result = requests.HandleLogoutUser(username, password);

                        string result_s = JsonSerializer.Serialize(result);
                        byte[] result_a = Encoding.ASCII.GetBytes(result_s);
                        SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0, ref stream);
                        stream.Write(result_a, 0, result_a.Length);
                    }
                }
                else
                {
                    SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0, ref stream);
                }

                client.Close();
            }
        }

        private byte[] GetContent(string requestedPath)
        {
            if (requestedPath == "/")
            {
                requestedPath = "index.html";
            }
            string filePath = Path.Join(WebServerPath, requestedPath);

            if (!File.Exists(filePath))
            {
                return null;
            }
            else
            {
                byte[] file = System.IO.File.ReadAllBytes(filePath);
                return file;
            }
        }

        private (Dictionary<string, string> headers, string requestType) ParseHeaders(string headerString)
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

        private void SendHeaders(string? httpVersion, int statusCode, string statusMsg, string? contentType, string? contentEncoding,
            int byteLength, ref NetworkStream networkStream)
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
            networkStream.Write(responseBytes, 0, responseBytes.Length);
        }
    }
}
