using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Akasztofa
{
    internal class Game
    {
        private const string ValidInputs = "!&'*,./0123456789:;<>?\\abcdefghijklmnopqrstuvwxyz~­áäéëíóöúüőťű";
        private string[]? words_array = null;

        private DatabaseConnection dbc;
        private User? user = null;
        private Config.ConfigData configData;

        private Game()
        {
            configData = Config.LoadConfigData("config.json");
            dbc = new DatabaseConnection(configData.serverIP, configData.serverPort);

            DatabaseConnection.ClientConnectRequest? result = dbc.ConnectClient(configData.clientID);
            if(result != null)
            {
                dbc.connectionID = result.connectionID.ToString();

                RSAParameters parameters = new RSAParameters();
                parameters.Exponent = result.exponent;
                parameters.Modulus = result.modulus;
                dbc.rsa = RSA.Create(parameters);
            }
        }

        private static Game? instance;
        public static Game GetInstance()
        {
            if(instance == null)
            {
                Console.CancelKeyPress += OnCancelKeyPress;
                instance = new Game();
            }

            return instance;
        }

        public bool Init()
        {
            user = User.LoginMethod(dbc);
            return user != null;
        }

        static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            GetInstance().Shutdown();
            Environment.Exit(0);
        }

        public void Shutdown()
        {
            dbc.LogoutUser(user!.SessionID);
            dbc.DisconnectClient(dbc.connectionID);
        }

        public void Run()
        {
            if(!Init())
            {
                Console.WriteLine("ERROR: couldn't init the user!");
                return;
            }

            bool keep_running = true;

            while(keep_running && (user != null))
            {
                PlayRound();

                Console.Write("Keep playing? (y/n) ");
                string input = Console.ReadLine()!;
                if(input == "n" || input == "no" || input == "quit" || input == "exit" || input == "0")
                {
                    keep_running = false;
                }
            }

            Shutdown();
        }

        void PlayRound()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);

            string word = GetWord();
            List<char> guesses = new List<char>();
            guesses.Add(' ');
            guesses.Add('-');

            int bad_guesses = 0;
            bool guessed = false;
            do
            {
                Console.SetCursorPosition(Console.BufferWidth - 15, 0);
                Console.Write("Bad guesses: {0}", bad_guesses);
                Console.SetCursorPosition(Console.BufferWidth - 19, 1);
                Console.Write("High score: {0}", user!.GetHighscore());
                Console.SetCursorPosition(0, 0);

                Draw(word, guesses);

                Console.Write("Guesses so far: ");
                foreach (char ch in guesses)
                {
                    Console.Write("{0} ", ch);
                }
                Console.WriteLine();

                bool correct;
                string characters;
                do
                {
                    correct = false;
                    Console.WriteLine("Enter one or more characters: ");
                    characters = Console.ReadLine()!;
                    if(characters != "")
                    {
                        correct = true;
                    }
                }
                while (!correct);

                foreach (char c in characters)
                {
                    if(ValidInputs.Contains(c))
                    {
                        if (!guesses.Contains(c))
                        {
                            guesses.Add(c);

                            if (!word.Contains(c))
                            {
                                bad_guesses++;
                            }
                        }
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
            dbc.UpdateUser(user.SessionID, user.GetEncryptedData());

            Console.ForegroundColor = ConsoleColor.Green;

            Console.SetCursorPosition(Console.BufferWidth - 15, 0);
            Console.Write("Bad guesses: {0}", bad_guesses);
            Console.SetCursorPosition(Console.BufferWidth - 19, 1);
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

        bool ValidInput(char c)
        {
            return ValidInputs.Contains(c);
        }

        //Draws out the word, returns true if we guessed every character of the word, false otherwise
        bool Draw(string word, List<char> guesses)
        {
            bool complete = true;

            foreach (char c in word)
            {
                if (guesses.Contains(c))
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

        string GetWord()
        {
            if(words_array == null)
            {
                if (File.Exists("magyar-szavak.txt"))
                {
                    string words = File.ReadAllText("magyar-szavak.txt");
                    words_array = words.Split('\n');
                }
                else
                {
                    return "hiányzószavakfájl";
                }
            }

            string word = words_array[new Random().Next(words_array.Length)].ToLower();
            if(word.Last() == '\r')
            {
                word = word.Substring(0, word.Length - 1);
            }

            return word;
        }
    }
}
