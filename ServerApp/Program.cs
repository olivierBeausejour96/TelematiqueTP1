using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TCPCopycat;

namespace ServerApp
{

    class Program
    {
        private const int listenPort = 11000;
        static void Main(string[] args)
        {
            Console.WriteLine("Main Server App");
            TCPCopyCatInterface instance = new TCPCopyCatInterface();
            instance.initialize(11000);
            instance.startListening();
            
            do
            {
                Console.WriteLine("Enter `q` to quit gracefully");
            } while (Console.ReadLine().ToLower() != "q");

            

            /*
            string data = "HelloWorld";

            byte[] byteArrayData = Encoding.ASCII.GetBytes(data);
            int sizeOfData = byteArrayData.Length;
            int packetNumber = 1;

            TCPCopycatPacket qwe = new TCPCopycatPacket(new TCPCopycatPacket.Header { dataLenght = sizeOfData, packetNumber = packetNumber }, byteArrayData);

            byte[] parseByteArray = qwe.toByte();

            TCPCopycatPacket qwe2 = TCPCopycatPacket.parse(parseByteArray);

            string das = Encoding.UTF8.GetString(qwe2.data);


            TCPCopycatPacket[] asd = TCPCopycatPacketManager.FileToTCPCopycatPacket(@"C:\Users\beao3002\Desktop\qwe.zip");

            TCPCopycatPacketManager.TCPCopycatPacketArrayToFile(@"C:\Users\beao3002\Desktop\qwe2.zip", asd);

            int i = 2 + 2;
            */
        }
    }
}
