using Akasztofa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HangmanServer
{
    internal class Requests
    {
        public class RequestResult
        {
            public bool result { get; set; }
        }

        public class UserExistsRequest : RequestResult { }

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

        public class UserUpdateRequest : RequestResult { }
        public class UserLogoutRequest : RequestResult { }

        private UserDatabase database;

        public Requests(string db_location)
        {
            database = new UserDatabase(db_location);
        }

        public UserExistsRequest HandleUserExists(string username)
        {
            UserExistsRequest result = new();
            result.result = database.UserExists(username);
            return result;
        }

        public UserLoginRequest HandleUserLogin(string username, string password, out User? user)
        {
            user = null;
            if (database.UserExists(username))
            {
                string pass_try = database.SecurePassword(database.GetUserID(username), password);
                string hash = Crypto.GetHashString(pass_try);
                database.TryLogin(username, hash, out user);
            }

            UserLoginRequest result = new();
            result.result = false;
            result.userID = "";
            result.key = "";
            result.data = "";

            if (user != null)
            {
                result.result = true;
                result.userID = user.ID;
                result.key = user.encryption_key;
                result.data = user.data_encrypted;
            }

            return result;
        }

        public UserUpdateRequest HandleUpdateUser(User? user, string data)
        {
            UserUpdateRequest result = new();
            result.result = false;

            if (user != null)
            {
                user.data_encrypted = data;
                user.SaveData();
                result.result = true;
            }

            return result;
        }

        public UserCreationRequest HandleCreateUser(string username, string password)
        {
            User? user = null;
            string secure_pass = database.SecurePassword(database.GetUserID(username), password);
            bool res = database.CreateNewUser(username, secure_pass, out user);

            UserCreationRequest result = new();
            result.result = false;
            result.userID = "";
            result.key = "";
            result.data = "";

            if (user != null)
            {
                result.result = true;
                result.userID = user.ID;
                result.key = user.encryption_key;
                result.data = user.data_encrypted;
            }

            return result;
        }
    }
}
