using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangmanServer
{
    internal class Words
    {
        private static string[]? words_array;
        private static string words_file = "/magyar-szavak.txt";
        public static string GetWord()
        {
            if (words_array == null)
            {
                if (File.Exists(Connection.WebServerPath + words_file))
                {
                    string words = File.ReadAllText(Connection.WebServerPath + words_file);
                    words_array = words.Split('\n');
                }
                else
                {
                    return "hiányzószavakfájl";
                }
            }

            string word = words_array[new Random().Next(words_array.Length)].ToLower();
            if (word.Last() == '\r')
            {
                word = word.Substring(0, word.Length - 1);
            }

            return word;
        }
    }
}
