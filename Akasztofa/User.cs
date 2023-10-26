using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

//UserDatabase -> loads the users in
//We select a user, try to validate it, if it works -> create a User object
//Use the User object to manipulate the user data during the game

namespace Akasztofa
{
    internal class User
    {
        public const int ID_length = 15;
        private class PreviousWord
        {
            public string word { get; set; }
            public int bad_tries { get; set; }
            public List<char> guesses { get; set; }
        }

        private class PlayerDataInternal
        {
            public string username { get; set; }
            public int highscore { get; set; }
            public List<PreviousWord> words { get; set; }
        }

        public string ID { get; }
        public string username {  get; }
        public string password_hash2 { get; set; }
        public string encryption_key { get; set; }

        private string decrypted_key = "";
        private PlayerDataInternal data = new();

        //Logging an existing user in
        public User(UserDatabase.JSONData json, string password_hash1)
        {
            this.username = json.username;
            this.ID = json.user_id;
            this.password_hash2 = json.password_hash2;
            this.encryption_key = json.encrypted_key;
            this.decrypted_key = Crypto.Decrypt(json.encrypted_key, password_hash1, ID);

            string filepath = "players/" + username;
            if (File.Exists(filepath))
            {
                string contents = File.ReadAllText(filepath);
                string decrypted = Crypto.Decrypt(contents, decrypted_key, ID);   
                data = JsonSerializer.Deserialize<PlayerDataInternal>(decrypted);
            }
            else
            {
                data.username = username;
                data.highscore = 0;
                data.words = new();
            }
        }

        //
        public User(string ID, string username, string password_hash1)
        {
            this.ID = ID;
            this.username = username;

            GenerateUserIdentification(password_hash1);
                        
            data.username = username;
            data.highscore = 0;
            data.words = new();
        }

        public void SaveData()
        {
            string contents = JsonSerializer.Serialize(data);
            string encrypted = Crypto.Encrypt(contents, decrypted_key, ID);
            File.WriteAllText("players/" + username, encrypted);
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
        }

        public void AddRecord(string word, int bad_tries, List<char> guesses)
        {
            data.words.Add(new PreviousWord { word = word, bad_tries = bad_tries, guesses = guesses });
        }

        public int GetHighscore()
        {
            return data.highscore;
        }

        public void AddHighscore(int scored)
        {
            data.highscore += scored;
        }
    }
}
