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

/**
 * SERVER Operations: 
 * 
 * -    user exists query: GET ?type=exists&username=_________; returns: result: bool
 * -    user creation query: POST ?type=create&username=_________&password=_________; returns: result: bool, id: string, key: string, PlayerData: encrypted string
 * -    user login query: GET ?type=login&username=_________&password=_________; returns: result: bool, id: string, key: string, PlayerData: encrypted string
 * -    user update query: POST ?type=update&username=_________&password=_________&data=_________; returns: result: bool
 * -    user logout query: POST ?type=logout&username=_________&password=_________; returns: result: bool
 * */

namespace Akasztofa
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Game game = Game.GetInstance();
            game.Run();
        }
    }
}