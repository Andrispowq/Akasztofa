﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
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
            public string userID { get; set; } = "";
            public string key { get; set; } = "";
            public string data { get; set; } = "";
        }

        private HttpClient client;
        private NetworkStream stream;

        public DatabaseConnection(string databaseIP, int port)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            client = new HttpClient();
            client.BaseAddress = new Uri("http://" + databaseIP + ":" + port);
        }

        public UserExistsRequest? UserExists(string username)
        {
            string query = "?type=exists&username=" + username;
            string result = GetRequest(query);
            return JsonSerializer.Deserialize<UserExistsRequest>(result);
        }

        public UserCreationRequest? CreateUser(string username, string password)
        {
            string query = "?type=create&username=" + username + "&password=" + password;
            string result = GetRequest(query);
            return JsonSerializer.Deserialize<UserCreationRequest>(result);
        }

        public UserLoginRequest? LoginUser(string username, string password)
        {
            string query = "?type=login&username=" + username + "&password=" + password;
            string result = GetRequest(query);
            return JsonSerializer.Deserialize<UserLoginRequest>(result);
        }

        public UserUpdateRequest? UpdateUser(string username, string password, string data_encrypted)
        {
            /*tcpClient = new TcpClient("127.0.0.1", 6969);
            networkStream = tcpClient.GetStream();

            byte[] buffer = Encoding.ASCII.GetBytes(data_encrypted);
            networkStream.Write(buffer, 0, buffer.Length);
            networkStream.Close();*/

            string query = "?type=update&username=" + username + "&password=" + password + "&data=" + data_encrypted;
            string result = GetRequest(query);
            return JsonSerializer.Deserialize<UserUpdateRequest>(result);
        }

        public UserLogoutRequest? LogoutUser(string username, string password)
        {
            string query = "?type=logout&username=" + username + "&password=" + password;
            string result = GetRequest(query);
            return JsonSerializer.Deserialize<UserLogoutRequest>(result);
        }

        private string GetRequest(string query)
        {
            Task<HttpResponseMessage> response = client.GetAsync(query);
            while (!response.IsCompleted);

            Task<string> result = response.Result.Content.ReadAsStringAsync();
            while (!result.IsCompleted);

            return result.Result;
        }
    }
}
