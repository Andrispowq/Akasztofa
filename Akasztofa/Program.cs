/*
 * ---------------------------- FONTOS ----------------------------
 *          A program optimális működéséhez szükséges egy
 *       'magyar-szavak.txt' fájl megléte, mely a futtatható
 *        állomány mappájában kell, hogy megtalálható legyen
 *        Ezen fájl hiányában a program csak egyetlen szóval
 *      fog működni, és nem fog tudni saját szavakat generálni.
 *     Ez a fájl megtalálható a csatolt .zip fájlban mellékelve.
 * ---------------------------- FONTOS ----------------------------
 * */


using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Numerics;

using System.Security.Cryptography;
using System.Text;

namespace Akasztofa
{
    internal class Program
    {
        const string ValidInputs = "aábcdeéfghiíjklmnoóöőpqrstuúüűvwxyz";

        static string GetWord()
        {
            if (File.Exists("magyar-szavak.txt"))
            {
                string words = File.ReadAllText("magyar-szavak.txt");
                string[] words_array = words.Split('\n');
                return words_array[new Random().Next(words_array.Length)].ToLower();
            }
            else
            {
                return "hiányzószavakfájl";
            }
        }

        //Draws out the word, returns true if we guessed every character of the word, false otherwise
        static bool Draw(string word, List<char> guesses)
        {
            bool complete = true;

            foreach(char c in word)
            {
                if(guesses.Contains(c))
                {
                    Console.Write("{0} ", c);
                }
                else
                {
                    complete = false;
                    Console.Write("_ ");
                }
            }

            Console.WriteLine();
            return complete;
        }

        static bool ValidInput(char c)
        {
            return ValidInputs.Contains(c);
        }

        static void Main(string[] args)
        {
            UserDatabase database = new UserDatabase("user_database.json");
            Console.WriteLine("Please enter your username: ");
            string username = Console.ReadLine()!;

            string decrypted_key = "";

            string password_hash2 = database.GetPasswordHashFromUsername(username);
            if(password_hash2 == "")
            {
                Console.WriteLine("Enter password for new profile: ");
                string password = Console.ReadLine()!;
                string password_hash = Crypto.GetHashString(password);
                password_hash2 = Crypto.GetHashString(password_hash);

                long currentTimeMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                int seed = (int)(currentTimeMillis % int.MaxValue);
                Random random = new Random(seed);
                byte[] key = new byte[128];
                for(int i = 0; i < key.Length; i++)
                {
                    key[i] = (byte)random.Next(256);
                }

                decrypted_key = key.ToString()!;
                string encrypted_key = Crypto.Encrypt(decrypted_key, password_hash);
                database.AddUser(username, password_hash2, encrypted_key);
                database.FlushJSON();
            }
            else
            {
                bool success = false;
                for (int i = 0; i < 3 && !success; i++)
                {
                    Console.WriteLine("Please enter your password: ");
                    string pass_try = Console.ReadLine()!;
                    string hash = Crypto.GetHashString(pass_try);
                    string hash2 = Crypto.GetHashString(hash);

                    if (hash2 != password_hash2)
                    {
                        Console.WriteLine("Password doesn't match! Try again ({0} tries left)!", (3 - i - 1));
                    }
                    else
                    {
                        decrypted_key = Crypto.Decrypt(database.GetEncryptedKeyHashFromUsername(username), hash);
                        success = true;
                    }
                }

                if(!success)
                {
                    Console.WriteLine("Your password didn't match!");
                    return;
                }
            }

            if (!FileSystem.DirectoryExists("players"))
            {
                FileSystem.CreateDirectory("players");
            }

            PlayerData playerData = new PlayerData("players/" + username, decrypted_key);

            Console.Clear();
            Console.SetCursorPosition(0, 0);

            string word = GetWord();
            List<char> guesses = new();

            int bad_guesses = 0;
            bool guessed = false;
            do
            {
                Console.SetCursorPosition(Console.BufferWidth - 15, 0);
                Console.Write("Bad guesses: {0}", bad_guesses);
                Console.SetCursorPosition(Console.BufferWidth - 18, 1);
                Console.Write("High score: {0}", playerData.GetHighscore());
                Console.SetCursorPosition(0, 0);

                Draw(word, guesses);

                Console.Write("Guesses so far: ");
                foreach(char ch in guesses)
                {
                    Console.Write("{0} ", ch);
                }
                Console.WriteLine();

                bool correct;
                char c;
                do
                {
                    correct = true;
                    Console.WriteLine("Enter a character: ");
                    correct &= char.TryParse(Console.ReadLine(), out c);
                    correct &= ValidInput(c);
                }
                while (!correct);

                if(!guesses.Contains(c))
                {
                    guesses.Add(c);

                    if(!word.Contains(c))
                    {
                        bad_guesses++;
                    }
                }

                if (Draw(word, guesses))
                {
                    guessed = true;
                }

                Console.Clear();
                Console.SetCursorPosition(0, 0);
            }
            while (!guessed);

            playerData.AddWord(word, bad_guesses, guesses);
            playerData.AddHighscore((44 - bad_guesses) * 10 + word.Length);
            playerData.FlushJSON();

            Console.ForegroundColor = ConsoleColor.Green;

            Console.SetCursorPosition(Console.BufferWidth - 15, 0);
            Console.Write("Bad guesses: {0}", bad_guesses);
            Console.SetCursorPosition(Console.BufferWidth - 18, 1);
            Console.Write("High score: {0}", playerData.GetHighscore());
            Console.SetCursorPosition(0, 0);

            Draw(word, guesses);

            Console.Write("Guesses so far: ");
            foreach (char ch in guesses)
            {
                Console.Write("{0} ", ch);
            }
            Console.WriteLine();

            Console.WriteLine("Congrats!");
            Console.WriteLine("Guessed the word with {0} bad guesses.", bad_guesses);

            Console.ResetColor();
        }
    }
}