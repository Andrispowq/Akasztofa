using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akasztofa
{
    internal class Game
    {
        private const string ValidInputs = "aábcdeéfghiíjklmnoóöőpqrstuúüűvwxyz-0123456789/";
        private string[] words_array = null;

        private User user;

        public Game(User user)
        { 
            this.user = user;
        }

        public void Run()
        {
            bool keep_running = true;

            while(keep_running)
            {
                PlayRound();

                Console.Write("Keep playing? (y/n) ");
                string input = Console.ReadLine()!;
                if(input == "n" || input == "no" || input == "quit" || input == "exit" || input == "0")
                {
                    keep_running = false;
                }
            }
        }

        void PlayRound()
        {
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
                foreach (char ch in guesses)
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

                if (!guesses.Contains(c))
                {
                    guesses.Add(c);

                    if (!word.Contains(c))
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

            return words_array[new Random().Next(words_array.Length)].ToLower();
        }
    }
}
