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

        static void Main(string[] args)
        {
            /*DatabaseConnection dbc = new DatabaseConnection("http://andris.cegkikoto.hu");
            string userexists = dbc.GetRequest("?username=test&password=testpwd");

            Format res = JsonSerializer.Deserialize<Format>(userexists);
            Console.WriteLine("Username {0} exists: {1}", res.username, res.result);*/

            UserDatabase database = new UserDatabase("user_database.json");
            User user = database.LoginMethod()!;

            Game game = new Game(user);
            game.Run();
        }
    }
}