using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Akasztofa
{
    internal class PlayerData
    {
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

        private PlayerDataInternal data = new PlayerDataInternal();
        private string fileName;
        private string key;

        public PlayerData(string fileName, string key)
        {
            this.fileName = fileName;
            this.key = key;

            if (File.Exists(fileName))
            {
                string encrypted = File.ReadAllText(fileName);
                string json = Crypto.Decrypt(encrypted, key);
                data = JsonSerializer.Deserialize<PlayerDataInternal>(json)!;
            }
            else
            {
                data.username = fileName.Substring(8, fileName.Length - 8); //without players/
                data.highscore = 0;
                data.words = new List<PreviousWord>();
            }
        }

        public void FlushJSON()
        {
            string decrypted = JsonSerializer.Serialize(data);
            string json = Crypto.Encrypt(decrypted, key);
            File.WriteAllText(fileName, json);
        }

        public void AddWord(string word, int bad_tries, List<char> guesses)
        {
            data.words.Add(new PreviousWord { word = word, bad_tries = bad_tries, guesses = guesses });
        }

        public int GetHighscore()
        {
            return data.highscore;
        }

        public void AddHighscore(int highscore)
        {
            data.highscore += highscore;
        }
    }
}
