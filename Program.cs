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
using System.Net;
using System.Numerics;

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

            Console.ForegroundColor = ConsoleColor.Green;

            Console.SetCursorPosition(Console.BufferWidth - 15, 0);
            Console.Write("Bad guesses: {0}", bad_guesses);
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