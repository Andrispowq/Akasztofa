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
            public string word { get; set; } = string.Empty;
            public int bad_tries { get; set; }
            public List<char> guesses { get; set; } = new();
        }

        private class PlayerDataInternal
        {
            public string username { get; set; } = string.Empty;
            public int highscore { get; set; }
            public List<PreviousWord> words { get; set; } = new();
        }

        public string ID { get; }
        public string username {  get; }
        public string password_hash2 { get; set; }
        public string encryption_key { get; set; }

        public string password = "";
        private string decrypted_key = "";
        private PlayerDataInternal data = new();

        //Logging an existing user in
        public User(DatabaseConnection.UserLoginRequest loginInfo, string username, string password)
        {
            string hash1 = Crypto.GetHashString(password);
            string hash2 = Crypto.GetHashString(hash1);

            this.ID = loginInfo.userID;
            this.username = username;
            this.password = password;
            this.password_hash2 = hash2;
            this.encryption_key = loginInfo.key;
            this.decrypted_key = Crypto.Decrypt(loginInfo.key, hash1, ID);

            if (loginInfo.data != "")
            {
                string decrypted = Crypto.Decrypt(loginInfo.data, decrypted_key, ID);
                data = JsonSerializer.Deserialize<PlayerDataInternal>(decrypted)!;
            }
            else
            {
                data.username = username;
                data.highscore = 0;
                data.words = new();
            }
        }

        public static User? LoginMethod(DatabaseConnection dbc)
        {
            Console.Write("Please enter you username: ");
            string username = Console.ReadLine()!;
            DatabaseConnection.UserExistsRequest? exists = dbc.UserExists(username);
            if(exists == null)
            {
                return null;
            }

            if (exists.result)
            {
                for (int i = 0; i < 3; i++)
                {
                    Console.Write("Please enter your password: ");
                    string pass_try = Utils.GetPassword();

                    DatabaseConnection.UserLoginRequest? login = dbc.LoginUser(username, pass_try);
                    if (login != null)
                    {
                        if (login.result == true)
                        {
                            string secure_pass = User.SecurePassword(login.userID, pass_try);
                            return new User(login, username, secure_pass);
                        }
                    }

                    Console.WriteLine("Password did not match! ({0} tries left)", 3 - i);
                }
            }
            else
            {
                Console.Write("Enter password for new profile: ");
                string password = Utils.GetPassword();

                DatabaseConnection.UserCreationRequest? create = dbc.CreateUser(username, password);
                if (create != null)
                {
                    if(create.result == true)
                    {
                        DatabaseConnection.UserLoginRequest login = new();
                        login.result = create.result;
                        login.userID = create.userID;
                        login.key = create.key;
                        login.data = create.data;

                        string secure_pass = User.SecurePassword(create.userID, password);
                        return new User(login, username, secure_pass);
                    }
                }
            }

            return null;
        }

        public static string SecurePassword(string userID, string password)
        {
            return password + userID;
        }

        public string GetEncryptedData()
        {
            string decrypted = JsonSerializer.Serialize(data);
            return Crypto.Encrypt(decrypted, decrypted_key, ID);
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
