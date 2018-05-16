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

            TCPCopycatServerInterface instance = new TCPCopycatServerInterface();


            instance.initializeServer(11000);
            
            do
            {
                Console.WriteLine("Enter `q` to quit gracefully");
            } while (Console.ReadLine().ToLower() != "q");
            
            /*
            string data = "HelloWorld";

            byte[] byteArrayData = Encoding.ASCII.GetBytes(data);
            int sizeOfData = byteArrayData.Length;
            int packetNumber = 1;

            TCPCopycatPacket.TCPCopycatHeader tmpHeader = new TCPCopycatPacket.TCPCopycatHeader();
            tmpHeader.sequenceNumber = packetNumber;
            tmpHeader.dataLenght = sizeOfData;
        
            TCPCopycatPacket qwe = new TCPCopycatPacket(tmpHeader, byteArrayData);

            byte[] parseByteArray = qwe.serialize();

            TCPCopycatPacket qwe2 = TCPCopycatPacket.parse(parseByteArray);

            string das = Encoding.UTF8.GetString(qwe2.data);

            TCPCopycatPacket[] asd = TCPCopycatPacketManager.FileToTCPCopycatPacket(@"C:\Users\beao3002\Desktop\qwe.zip");

            TCPCopycatPacketManager.TCPCopycatPacketArrayToFile(@"C:\Users\beao3002\Desktop\qwe2.zip", asd);

            int i = 2 + 2;
            */
        }
    }
}
