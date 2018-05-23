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

        static void Main(string[] args)
        {
            Console.WriteLine("Client app");

            string fileToSend = @"C:\Users\beao3002\Desktop\qwe.zip";
            IPAddress serverIP = IPAddress.Loopback;
            int serverPort = 11000;

            if (args.Length != 0)
            {
                if (args.Length == 2)
                {
                    string[] qwe = args[0].Split(':');
                    serverIP = IPAddress.Parse(qwe[0]);
                    serverPort = Int32.Parse(qwe[1]);

                    fileToSend = args[1];
                }
                else
                {
                    Console.WriteLine("Bad number of args");
                    Console.WriteLine("Exiting app");
                    return;
                }
            }

            TCPCopycatClientInterface client = new TCPCopycatClientInterface();
            while (client.connectToServer(serverIP, serverPort).Result != TCPCopyCatController.responseCode.OK);



            client.sendFile(fileToSend);
            Console.WriteLine("Done transfering");
            Console.ReadLine();
        }
    }
}
