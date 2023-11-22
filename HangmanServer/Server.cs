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
using System.Collections;
using System.Data.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Concurrent;

namespace HangmanServer
{
    internal class Server
    {
        private Connection connection;
        private Requests requests;
        private Config config;

        private ConcurrentDictionary<Guid, Session> sessions;
        private bool exitThread = false;

        public Server()
        {
            config = Config.GetInstance();
            config.config = Config.LoadConfigData("config.json");
            Connection.WebServerPath = config.config.serverFolder;
            connection = new Connection(config.config.serverIP, config.config.serverPort);
            requests = new Requests(Connection.WebServerPath + "/user_database.json");
            sessions = new ConcurrentDictionary<Guid, Session>();

            Thread th = new Thread(new ThreadStart(ListenerThread));
            th.Start();

            var then = DateTime.UtcNow;
            while(true)
            {
                var now = DateTime.UtcNow;
                var diff = (now - then);
                double delta = diff.TotalSeconds;
                then = DateTime.UtcNow;

                string? input = Console.ReadLine();
                if(input != null && input != "")
                {
                    if(input == "q" || input == "quit" || input == "e" || input == "exit")
                    {
                        exitThread = true;
                        th.Join();
                        connection.Close();
                        break;
                    }
                }

                List<Guid> timedouts = new List<Guid>();
                foreach(var session in sessions)
                {
                    session.Value.Update(delta);
                    if(session.Value.IsTimedOut())
                    {
                        timedouts.Add(session.Key);
                    }
                }

                foreach(var ID in timedouts)
                {
                    Console.WriteLine("Timed out session (name: {0}, sessionID: {1})", sessions[ID].GetUserData().username, ID);
                    Session? session;
                    sessions.Remove(ID, out session);
                }
            }
        }

        private void ListenerThread()
        {
            while (!exitThread)
            {
                string request = connection.GetRequest();

                //string request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
                var requestHeaders = Utils.ParseHeaders(request);

                string[] requestFirstLine = requestHeaders.requestType.Split(" ");
                string httpVersion = requestFirstLine.LastOrDefault()!;
                string contentType = requestHeaders.headers.GetValueOrDefault("Accept")!;
                string contentEncoding = requestHeaders.headers.GetValueOrDefault("Acept-Encoding")!;

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
                    if (type != null)
                    {
                        if (type == "exists")
                        {
                            string? username = parsed.Get("username");
                            if(username != null)
                            {
                                UserExistsRequest result = requests.HandleUserExists(username);

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                connection.WriteStream(JsonSerializer.Serialize(result));
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the username");
                            }
                        }
                        else if(type == "login")
                        {
                            string? username = parsed.Get("username");
                            string? password = parsed.Get("password");
                            if (username != null && password != null)
                            {
                                bool found = false;
                                foreach(var session in sessions)
                                {
                                    if(session.Value.GetUserData().username == username)
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    User? user;
                                    UserLoginRequest result = requests.HandleUserLogin(username, password, out user);

                                    if (user != null)
                                    {
                                        Session newSession = new Session(user);
                                        result.sessionID = newSession.GetSessionID();
                                        sessions.TryAdd(newSession.GetSessionID(), newSession);
                                    }

                                    connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                    connection.WriteStream(JsonSerializer.Serialize(result));
                                }
                                else
                                {
                                    connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                    connection.WriteStream("ERROR: username " + username + " is already in an active session!");
                                }
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the username and password");
                            }
                        }
                        else if(type == "create")
                        {
                            string? username = parsed.Get("username");
                            string? password = parsed.Get("password");
                            if (username != null && password != null)
                            {
                                UserCreationRequest result = requests.HandleCreateUser(username, password);

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                connection.WriteStream(JsonSerializer.Serialize(result));
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the username and password");
                            }
                        }
                        else if(type == "update")
                        {
                            string? sessionID = parsed.Get("sessionID");
                            string? data = parsed.Get("data");
                            if (sessionID != null && data != null)
                            {
                                Guid guid = Guid.Parse(sessionID);
                                if(sessions.ContainsKey(guid))
                                {
                                    Session session = sessions[guid];
                                    session.RefreshSession();

                                    User user = session.GetUserData();
                                    UserUpdateRequest result = requests.HandleUpdateUser(user, data);

                                    connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                    connection.WriteStream(JsonSerializer.Serialize(result));
                                }
                                else
                                {
                                    connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                    connection.WriteStream("ERROR: bad sessionID");
                                }
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the sessionID and data");
                            }
                        }
                        else if(type == "logout")
                        {
                            string? sessionID = parsed.Get("sessionID");
                            if (sessionID != null)
                            {
                                Guid guid = Guid.Parse(sessionID);
                                if (sessions.ContainsKey(guid))
                                {
                                    Session? session = sessions[guid];
                                    User user = session.GetUserData();

                                    UserLogoutRequest result = new UserLogoutRequest();
                                    result.result = true;
                                    sessions.Remove(guid, out session);

                                    connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                    connection.WriteStream(JsonSerializer.Serialize(result));
                                }
                                else
                                {
                                    connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                    connection.WriteStream("ERROR: bad sessionID");
                                }
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the sessionID");
                            }
                        }
                        else
                        {
                            connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                            connection.WriteStream("ERROR: specify the correct type, not " + type);
                        }
                    }
                    else
                    {
                        connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                        connection.WriteStream("ERROR: specify the method type");
                    }

                }
                else
                {
                    connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                }

                connection.CloseActiveClient();
            }
        }

        private byte[]? GetContent(string requestedPath)
        {
            if (requestedPath == "/")
            {
                requestedPath = "index.html";
            }
            string filePath = Path.Join(Connection.WebServerPath, requestedPath);

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
    }
}
