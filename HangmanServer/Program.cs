using System.Net.Sockets;
using System.Net;
using System.Text;

namespace HangmanServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server("127.0.0.1", 6969);
        }
    }
}