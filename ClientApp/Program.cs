using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TCPCopycat;

namespace ClientApp
{
    class Program
    {
        private const int serverListenPort = 11000;
        private static bool terminateProgram = false;
        private static string serverIP = "127.0.0.1";

        static void Main(string[] args)
        {
            Console.WriteLine("Client app");
            TCPCopycatClientInterface client = new TCPCopycatClientInterface();
            client.connectToServer(IPAddress.Loopback, serverListenPort);
            Console.ReadLine();
        }
    }
}
