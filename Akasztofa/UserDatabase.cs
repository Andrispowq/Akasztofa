using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Akasztofa
{
    internal class UserDatabase
    {
        private class JSONData
        {
            public string username { get; set; }
            public string password_hash { get; set; }
            public string encrypted_key { get; set; }
        }

        private List<JSONData> data = new List<JSONData>();
        private string fileName;

        public UserDatabase(string fileName)
        {
            this.fileName = fileName;

            if (File.Exists(fileName))
            {
                string json = File.ReadAllText(fileName);
                data = JsonSerializer.Deserialize<List<JSONData>>(json)!;
            }
        }

        public void FlushJSON()
        {
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(fileName, json);
        }

        public void AddUser(string username, string password_hash, string encrypted_key)
        {
            data.Add(new JSONData { username = username, password_hash = password_hash, encrypted_key = encrypted_key });
        }

        public string GetPasswordHashFromUsername(string username)
        {
            foreach(JSONData dat in data)
            {
                if(dat.username == username)
                {
                    return dat.password_hash;
                }
            }

            return "";
        }

        public string GetEncryptedKeyHashFromUsername(string username)
        {
            foreach (JSONData dat in data)
            {
                if (dat.username == username)
                {
                    return dat.encrypted_key;
                }
            }

            return "";
        }
    }
}
