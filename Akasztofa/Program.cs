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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Akasztofa
{
    internal class Program
    {
        const string ValidInputs = "aábcdeéfghiíjklmnoóöőpqrstuúüűvwxyz-0123456789/";

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

        static string GetPassword()
        {
            string pass = "";
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            return pass;
        }

        static bool ValidInput(char c)
        {
            return ValidInputs.Contains(c);
        }
        private struct Format
        {
            public bool result { get; set; }
            public string? username { get; set; }
        }

        static void Main(string[] args)
        {
            DatabaseConnection dbc = new DatabaseConnection("http://andris.cegkikoto.hu");
            string userexists = dbc.GetRequest("?username=test&password=testpwd");

            Format res = JsonSerializer.Deserialize<Format>(userexists);
            Console.WriteLine("Username {0} exists: {1}", res.username, res.result);

            UserDatabase database = new UserDatabase("user_database.json");
            Console.Write("Please enter your username: ");
            string username = Console.ReadLine()!;

            User? user = null;
            if(database.UserExists(username))
            {
                bool success = false;
                for (int i = 0; i < 3 && !success; i++)
                {
                    Console.Write("Please enter your password: ");
                    string pass_try = database.SecurePassword(database.GetUserID(username), GetPassword());
                    string hash = Crypto.GetHashString(pass_try);
                    if(!database.TryLogin(username, hash, out user))
                    {
                        Console.WriteLine("Password doesn't match! Try again ({0} tries left)!", (3 - i - 1));
                    }
                    else
                    {
                        success = true;
                    }
                }

                if (!success)
                {
                    Console.WriteLine("Your password didn't match!");
                    return;
                }
            }
            else
            {
                Console.Write("Enter password for new profile: ");
                string password = GetPassword();                
                if(!database.CreateNewUser(username, password, out user))
                {
                    Console.WriteLine("ERROR: user creation failed (maybe it already exists?)!\n");
                }

                database.SaveData();
            }

            if (!FileSystem.DirectoryExists("players"))
            {
                FileSystem.CreateDirectory("players");
            }

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
                Console.SetCursorPosition(Console.BufferWidth - 19, 1);
                Console.Write("High score: {0}", user.GetHighscore());
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

            user.AddRecord(word, bad_guesses, guesses);
            user.AddHighscore((44 - bad_guesses) * 10 + word.Length);
            user.SaveData();

            Console.ForegroundColor = ConsoleColor.Green;

            Console.SetCursorPosition(Console.BufferWidth - 15, 0);
            Console.Write("Bad guesses: {0}", bad_guesses);
            Console.SetCursorPosition(Console.BufferWidth - 19 , 1);
            Console.Write("High score: {0}", user.GetHighscore());
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