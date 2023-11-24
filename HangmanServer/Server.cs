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
using static System.Collections.Specialized.BitVector32;
using System.Threading;
using System.Security.Cryptography;

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
            Logger.InitialiseLogger();

            sessions = new ConcurrentDictionary<Guid, Session>();

            Thread th = new Thread(new ThreadStart(ListenerThread));
            th.Start();

            Console.WriteLine("Web Server Running on {0}:{1}... To exit, press the 'q' or 'e' keys...", config.config.serverIP, config.config.serverPort);

            var then = DateTime.UtcNow;
            while (true)
            {
                var now = DateTime.UtcNow;
                var diff = (now - then);
                double delta = diff.TotalSeconds;
                then = DateTime.UtcNow;

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.Q:
                        case ConsoleKey.E:
                            exitThread = true;
                            th.Join();
                            Logger.ShutdownLogger();
                            connection.Close();
                            break;
                        default:
                            break;
                    }
                }

                if(exitThread)
                {
                    break;
                }

                List<Guid> timeouts = new List<Guid>();
                foreach (var session in sessions)
                {
                    session.Value.Update(delta);
                    if (session.Value.IsTimedOut())
                    {
                        timeouts.Add(session.Key);
                    }
                }

                foreach (var ID in timeouts)
                {
                    Console.WriteLine("Timed out session (sessionID: {0})", ID);
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
                if(request == "")
                {
                    continue;
                }

                //string request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);
                var requestHeaders = Utils.ParseHeaders(request);

                string[] requestFirstLine = requestHeaders.requestType.Split(" ");
                string httpVersion = requestFirstLine.LastOrDefault()!;
                string contentType = requestHeaders.headers.GetValueOrDefault("Accept")!;
                string contentEncoding = requestHeaders.headers.GetValueOrDefault("Acept-Encoding")!;

                Logger.Log(0, "New request: " + request);

                if (request.StartsWith("GET"))
                {
                    string req = requestFirstLine[1];
                    if (req.StartsWith("/?"))
                    {
                        req = req.Substring(2);
                    }

                    var parsed = HttpUtility.ParseQueryString(req);
                    string? type = parsed.Get("type");
                    if (type != null)
                    {
                        if (type == "connect")
                        {
                            ClientConnectRequest result;
                            string? clientID = parsed.Get("clientID");
                            if (clientID != null)
                            {
                                result = new ClientConnectRequest();
                                result.result = false;

                                Guid clientId;
                                if (Guid.TryParse(clientID, out clientId))
                                {
                                    bool found = false;
                                    foreach (var session in sessions)
                                    {
                                        if (session.Value.GetClientID() == clientId)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }

                                    if (!found)
                                    {
                                        Session session = new Session(Guid.Parse(clientID));
                                        result.result = sessions.TryAdd(session.GetConnectionID(), session);
                                        result.connectionID = session.GetConnectionID();
                                        (byte[] exponent, byte[] modulus) = session.GetPublicKey();
                                        result.exponent = exponent;
                                        result.modulus = modulus;
                                    }
                                    else
                                    {
                                        result.message = "ERROR: client already connected!";
                                    }
                                }
                                else
                                {
                                    result.message = "ERROR: bad GUID";
                                }

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                connection.WriteStream(JsonSerializer.Serialize(result));
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the clientID");
                            }
                        }
                        else if (type == "disconnect")
                        {
                            string? connectionID = parsed.Get("connectionID");
                            string? noAnswer = parsed.Get("noanswer");
                            if (connectionID != null)
                            {
                                ClientDisconnectRequest result = new ClientDisconnectRequest();
                                result.result = false;

                                Guid connId;
                                if (Guid.TryParse(connectionID, out connId))
                                {
                                    if (sessions.ContainsKey(connId))
                                    {
                                        Session? session;
                                        sessions.Remove(connId, out session);
                                        result.result = true;
                                    }
                                    else
                                    {
                                        result.message = "ERROR: GUID not found";
                                    }
                                }
                                else
                                {
                                    result.message = "ERROR: bad GUID";
                                }

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                if (noAnswer != null)
                                {
                                    if(noAnswer != "true")
                                    {
                                        connection.WriteStream(JsonSerializer.Serialize(result));
                                    }
                                }
                                else
                                {
                                    connection.WriteStream(JsonSerializer.Serialize(result));
                                }
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the connectionID");
                            }
                        }
                        else if (type == "exists")
                        {
                            string? connectionID = parsed.Get("connectionID");
                            string? username = parsed.Get("username");
                            if (connectionID != null && username != null)
                            {
                                UserExistsRequest result = new UserExistsRequest();
                                result.result = false;

                                Guid connId;
                                if (Guid.TryParse(connectionID, out connId))
                                {
                                    if (sessions.ContainsKey(connId))
                                    {
                                        result = requests.HandleUserExists(username);
                                    }
                                    else
                                    {
                                        result.message = "ERROR: connectionID not found";
                                    }
                                }
                                else
                                {
                                    result.message = "ERROR: bad GUID";
                                }

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                connection.WriteStream(JsonSerializer.Serialize(result));
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the connectionID and username");
                            }
                        }
                        else if (type == "login")
                        {
                            string? connectionID = parsed.Get("connectionID");
                            string? username = parsed.Get("username");
                            string? password = parsed.Get("password");
                            string? plain = parsed.Get("plain");
                            if (connectionID != null && username != null && password != null)
                            {
                                UserLoginRequest result = new UserLoginRequest();
                                result.result = false;

                                Guid connId;
                                if (Guid.TryParse(connectionID, out connId))
                                {
                                    if (sessions.ContainsKey(connId))
                                    {
                                        Session? session = sessions[connId];
                                        if (session.GetSessionID() == Guid.Empty)
                                        {
                                            bool isPlain = false;
                                            if (plain != null)
                                            {
                                                isPlain = (plain == "true");
                                            }

                                            bool found = false;
                                            foreach (var sesh in sessions)
                                            {
                                                User? usr = sesh.Value.GetUserData();
                                                if (usr != null)
                                                {
                                                    if (usr.username == username)
                                                    {
                                                        found = true;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (!found)
                                            {
                                                User? user;
                                                result = requests.HandleUserLogin(session, username, password, out user, isPlain);

                                                if (user != null)
                                                {
                                                    session.LoginUser(user);
                                                    result.sessionID = session.GetSessionID();
                                                }
                                                else
                                                {
                                                    result.message = "ERROR: bad login info";
                                                }
                                            }
                                            else
                                            {
                                                result.message = "ERROR: user is already logged in";
                                            }
                                        }
                                        else
                                        {
                                            result.message = "ERROR: session already has a user logged in";
                                        }
                                    }
                                    else
                                    {
                                        result.message = "ERROR: connectionID not found";
                                    }
                                }
                                else
                                {
                                    result.message = "ERROR: bad GUID";
                                }

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                connection.WriteStream(JsonSerializer.Serialize(result));
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the connectionID, username and password");
                            }
                        }
                        else if (type == "create")
                        {
                            string? connectionID = parsed.Get("connectionID");
                            string? username = parsed.Get("username");
                            string? password = parsed.Get("password");
                            string? plain = parsed.Get("plain");
                            if (connectionID != null && username != null && password != null)
                            {
                                UserCreationRequest result = new UserCreationRequest();
                                result.result = false;

                                Guid connId;
                                if (Guid.TryParse(connectionID, out connId))
                                {
                                    if (sessions.ContainsKey(connId))
                                    {
                                        Session? session = sessions[connId];
                                        bool isPlain = false;
                                        if (plain != null)
                                        {
                                            isPlain = (plain == "true");
                                        }

                                        result = requests.HandleCreateUser(session, username, password, isPlain);
                                    }
                                    else
                                    {
                                        result.message = "ERROR: connectionID not found";
                                    }
                                }
                                else
                                {
                                    result.message = "ERROR: bad GUID";
                                }

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                connection.WriteStream(JsonSerializer.Serialize(result));
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the connectionID, username and password");
                            }
                        }
                        else if (type == "update")
                        {
                            string? sessionID = parsed.Get("sessionID");
                            string? data = parsed.Get("data");
                            if (sessionID != null && data != null)
                            {
                                UserUpdateRequest result = new UserUpdateRequest();
                                result.result = false;

                                Guid guid;
                                if (Guid.TryParse(sessionID, out guid))
                                {
                                    bool found = false;
                                    foreach (var session in sessions)
                                    {
                                        if (session.Value.GetSessionID() == guid)
                                        {
                                            session.Value.RefreshSession();

                                            User user = session.Value.GetUserData()!;
                                            result = requests.HandleUpdateUser(user, data);
                                            found = true;
                                            break;
                                        }
                                    }

                                    if(!found)
                                    {
                                        result.message = "ERROR: sessionID not found";
                                    }
                                }
                                else
                                {
                                    result.message = "ERROR: bad GUID";
                                }

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                connection.WriteStream(JsonSerializer.Serialize(result));
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the sessionID and data");
                            }
                        }
                        else if (type == "logout")
                        {
                            string? sessionID = parsed.Get("sessionID");
                            string? noAnswer = parsed.Get("noanswer");
                            if (sessionID != null)
                            {
                                UserLogoutRequest result = new UserLogoutRequest();
                                result.result = false;

                                Guid guid;
                                if (Guid.TryParse(sessionID, out guid))
                                {
                                    bool found = false;
                                    foreach (var session in sessions)
                                    {
                                        if (session.Value.GetSessionID() == guid)
                                        {
                                            session.Value.LogoutUser();
                                            found = true;
                                            break;
                                        }
                                    }

                                    if (!found)
                                    {
                                        result.message = "ERROR: sessionID not found";
                                    }
                                }
                                else
                                {
                                    result.message = "ERROR: bad GUID";
                                }

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                if (noAnswer != null)
                                {
                                    if (noAnswer != "true")
                                    {
                                        connection.WriteStream(JsonSerializer.Serialize(result));
                                    }
                                }
                                else
                                {
                                    connection.WriteStream(JsonSerializer.Serialize(result));
                                }
                            }
                            else
                            {
                                connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                                connection.WriteStream("ERROR: specify the sessionID");
                            }
                        }
                        else if (type == "wordrequest")
                        {
                            string? sessionID = parsed.Get("sessionID");
                            if (sessionID != null)
                            {
                                UserWordRequest result = new UserWordRequest();
                                result.result = false;

                                Guid guid;
                                if (Guid.TryParse(sessionID, out guid))
                                {
                                    bool found = false;
                                    foreach (var session in sessions)
                                    {
                                        if (session.Value.GetSessionID() == guid)
                                        {
                                            result = requests.HandleWordRequest();
                                        }
                                    }

                                    if (!found)
                                    {
                                        result.message = "ERROR: sessionID not found";
                                    }
                                }
                                else
                                {
                                    result.message = "ERROR: bad GUID";
                                }

                                connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                                connection.WriteStream(JsonSerializer.Serialize(result));
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
                        byte[]? content = GetContent(requestFirstLine[1]);
                        if (content != null)
                        {
                            connection.SendHeaders(httpVersion, 200, "OK", contentType, contentEncoding, 0);
                            connection.WriteStream(content);
                        }
                        else
                        {
                            connection.SendHeaders(httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, 0);
                        }
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
