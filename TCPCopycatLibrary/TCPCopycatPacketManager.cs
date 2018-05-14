using ClientApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp
{
    class TCPCopycatPacketManager
    {
        static int maxBytePerPacket = 1024;

        public static TCPCopycatPacket[] FileToTCPCopycatPacket(string filePath)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(@filePath);

            int nbPacket = fileBytes.Length / 1024 + 1;

            byte[][] ret = new byte[nbPacket][];
            
            for (int i = 0; i < nbPacket-1; ++i)
            {
                ret[i] = new byte[1024];
                for (int j = 0; j < 1024; ++j)
                {
                    ret[i][j] = fileBytes[i * 1024 + j];
                }
            }

            ret[nbPacket-1] = new byte[fileBytes.Length % 1024];

            for (int i = 0; i < fileBytes.Length % 1024; i++)
            {
                ret[nbPacket - 1][i] = fileBytes[(nbPacket - 1) * 1024 + i];
            }


            TCPCopycatPacket[] copycatPacketArray = new TCPCopycatPacket[nbPacket];

            for (int i = 0; i < nbPacket; i++)
            {
                copycatPacketArray[i] = new TCPCopycatPacket(new TCPCopycatPacket.Header { dataLenght = ret[i].Length, packetNumber = i }, ret[i]);
            }

            return copycatPacketArray;
        }

        public static void TCPCopycatPacketArrayToFile(string filePath, TCPCopycatPacket[] packetArray)
        {

            if (File.Exists(@filePath))
            {
                File.Delete(@filePath);
            }

            BinaryWriter bw;

            try
            {
                bw = new BinaryWriter(new FileStream(@filePath, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                for (int i = 0; i < packetArray.Length; i++)
                {
                    bw.Write(packetArray[i].data);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

    }
}
