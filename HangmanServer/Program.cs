using System.Net.Sockets;
using System.Net;
using System.Text;

namespace HangmanServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new Server();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}