using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HangmanServer;
using static System.Runtime.InteropServices.JavaScript.JSType;

//UserDatabase -> loads the users in
//We select a user, try to validate it, if it works -> create a User object
//Use the User object to manipulate the user data during the game

namespace Akasztofa
{
    internal class User
    {
        public const int ID_length = 15;

        public string ID { get; }
        public string username {  get; }
        public string password_hash2 { get; set; }
        public string encryption_key { get; set; }

        private string decrypted_key = "";
        public string data_encrypted;

        //Logging an existing user in
        public User(UserDatabase.JSONData json, string password_hash1)
        {
            this.username = json.username;
            this.ID = json.user_id;
            this.password_hash2 = json.password_hash2;
            this.encryption_key = json.encrypted_key;
            this.decrypted_key = Crypto.Decrypt(json.encrypted_key, password_hash1, ID);

            string filepath = Server.WebServerPath + "/players/" + username;
            if (File.Exists(filepath))
            {
                data_encrypted = File.ReadAllText(filepath);
            }
            else
            {
                data_encrypted = "";
            }
        }

        //
        public User(string ID, string username, string password_hash1)
        {
            this.ID = ID;
            this.username = username;
            this.password_hash2 = "";
            this.encryption_key = "";
            this.data_encrypted = "";

            GenerateUserIdentification(password_hash1);
        }

        public static string GenerateID(string username)
        {
            string id = "";

            Random random = new Random((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() ^ username.GetHashCode());
            for (int i = 0; i < ID_length; i++)
            {
                id += random.Next(256).ToString("X2");

                if ((((i + 1) % 5) == 0) && (i != ID_length - 1))
                {
                    id += '-';
                }
            }

            return id;
        }

        public void SaveData()
        {
            File.WriteAllText(Server.WebServerPath + "/players/" + username, data_encrypted);
        }

        public void GenerateUserIdentification(string password_hash1) 
        {
            this.password_hash2 = Crypto.GetHashString(password_hash1);

            long currentTimeMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            int seed = (int)(currentTimeMillis % int.MaxValue);
            Random random = new Random(seed);
            byte[] key = new byte[128];
            string key_string = "";
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (byte)random.Next(256);
                key_string += key[i].ToString("X2");
            }

            this.decrypted_key = key_string;
            this.encryption_key = Crypto.Encrypt(decrypted_key, password_hash1, ID);
            Console.WriteLine("Orignial: " + decrypted_key + ", Encrypted: " + encryption_key + ", Hash: " + password_hash1 + ", ID: " + ID);
        }
    }
}
