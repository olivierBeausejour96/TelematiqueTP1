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
            IPAddress serverIP = IPAddress.Parse("10.44.88.119");//IPAddress.Loopback;
            int serverPort = 11000;
            int options = TCPCopycatPacket.OPTION_UPLOAD;
            string option = "download";

            if (args.Length != 0)
            {
                if (args.Length == 3)
                {
                    string[] qwe = args[0].Split(':');
                    Console.WriteLine(qwe[0]);
                    Console.WriteLine(qwe[1]);
                    serverIP = IPAddress.Parse(qwe[0]);
                    option = args[1];
                    if (option.ToLower() == "upload")
                    {
                        options = TCPCopycatPacket.OPTION_UPLOAD;
                    }
                    else if (option.ToLower() == "download")
                    {
                        options = TCPCopycatPacket.OPTION_DOWNLOAD;
                    }
                    serverPort = Int32.Parse(qwe[1]);

                    fileToSend = args[2];
                }
                else
                {
                    Console.WriteLine("Bad number of args");
                    Console.WriteLine("Exiting app");
                    return;
                }
            }


            Console.WriteLine("");
            TCPCopycatClientInterface client = new TCPCopycatClientInterface();
            while (client.connectToServer(serverIP, serverPort, options, fileToSend).Result != TCPCopyCatController.responseCode.OK);


            if (option.ToLower() == "upload")
            {
                client.sendFile(fileToSend);
            }

            Console.ReadLine();
        }
    }
}
