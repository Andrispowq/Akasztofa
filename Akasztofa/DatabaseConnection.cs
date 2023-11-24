using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Akasztofa
{
    internal class DatabaseConnection
    {
        public class RequestResult
        {
            public bool result { get; set; }
            public string message { get; set; } = "";
        }

        public class UserExistsRequest : RequestResult { }
        public class UserUpdateRequest : RequestResult { }
        public class UserLogoutRequest : RequestResult { }

        public class UserCreationRequest : RequestResult
        {
            public string userID { get; set; } = "";
            public string key { get; set; } = "";
            public string data { get; set; } = "";
        }

        public class UserLoginRequest : RequestResult
        {
            public Guid sessionID { get; set; }
            public string userID { get; set; } = "";
            public string key { get; set; } = "";
            public string data { get; set; } = "";
        }
        public class ClientConnectRequest : RequestResult
        {
            public Guid connectionID { get; set; }
            public byte[]? exponent { get; set; }
            public byte[]? modulus { get; set; }
        }

        public class ClientDisconnectRequest : RequestResult { }

        public class UserWordRequest : RequestResult
        {
            public string word { get; set; } = "";
        }

        private HttpClient client;

        public string connectionID;
        public RSA rsa;

        public DatabaseConnection(string databaseIP, int port)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            client = new HttpClient();
            client.BaseAddress = new Uri("http://" + databaseIP + ":" + port);
        }

        public string EncryptPassword(string password)
        {
            byte[] pass_bytes = Encoding.Unicode.GetBytes(password);
            byte[] bytes = rsa.Encrypt(pass_bytes, RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(bytes);
        }

        public T TryDeserialize<T>(string json)
        {
            try
            {
                T? t = JsonSerializer.Deserialize<T>(json);
                if(t != null)
                {
                    return t;
                }
                else
                {
                    return default(T);
                }
            }
            catch(JsonException e)
            {
                return default(T);
            }
        }

        public ClientConnectRequest? ConnectClient(string clientID)
        {
            string query = "?type=connect&clientID=" + clientID;
            string result = GetRequest(query);
            return TryDeserialize<ClientConnectRequest>(result);
        }

        public ClientDisconnectRequest? DisconnectClient(string connectionID)
        {
            string query = "?type=disconnect&connectionID=" + connectionID;
            string result = GetRequest(query);
            return TryDeserialize<ClientDisconnectRequest>(result);
        }

        public UserExistsRequest? UserExists(string connectionID, string username)
        {
            string query = "?type=exists&connectionID=" + connectionID + "&username=" + username;
            string result = GetRequest(query);
            return TryDeserialize<UserExistsRequest>(result);
        }

        public UserCreationRequest? CreateUser(string connectionID, string username, string password)
        {
            string query = "?type=create&connectionID=" + connectionID + "&username=" + username + "&password=" + password;
            string result = GetRequest(query);
            return TryDeserialize<UserCreationRequest>(result);
        }

        public UserLoginRequest? LoginUser(string connectionID, string username, string password)
        {
            string query = "?type=login&connectionID=" + connectionID + "&username=" + username + "&password=" + password;
            string result = GetRequest(query);
            return TryDeserialize<UserLoginRequest>(result);
        }

        public UserUpdateRequest? UpdateUser(string sessionID, string data_encrypted)
        {
            string query = "?type=update&sessionID=" + sessionID + "&data=" + data_encrypted;
            string result = GetRequest(query);
            return TryDeserialize<UserUpdateRequest>(result);
        }

        public UserLogoutRequest? LogoutUser(string sessionID)
        {
            string query = "?type=logout&sessionID=" + sessionID;
            string result = GetRequest(query);
            return TryDeserialize<UserLogoutRequest>(result);
        }

        public UserWordRequest? RequestWord(string sessionID)
        {
            string query = "?type=wordrequest&sessionID=" + sessionID;
            string result = GetRequest(query);
            return TryDeserialize<UserWordRequest>(result);
        }

        private string GetRequest(string query)
        { 
            Task<HttpResponseMessage> response = client.GetAsync(query);
            while (!response.IsCompleted);

            if(response.Status == TaskStatus.Faulted)
            {
                return "";
            }

            Task<string> result = response.Result.Content.ReadAsStringAsync();
            while (!result.IsCompleted);

            return result.Result;
        }
    }
}
