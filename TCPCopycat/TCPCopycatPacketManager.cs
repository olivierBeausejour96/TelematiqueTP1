using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCopycat
{
    public class TCPCopycatPacketManager
    {
        static int maxBytePerPacket = 1024;

        public static TCPCopycatPacket[] FileToTCPCopycatPacket(string filePath)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(@filePath);

            int nbPacket = fileBytes.Length / maxBytePerPacket + 1;

            byte[][] ret = new byte[nbPacket][];
            
            for (int i = 0; i < nbPacket-1; ++i)
            {
                ret[i] = new byte[maxBytePerPacket];
                for (int j = 0; j < maxBytePerPacket; ++j)
                {
                    ret[i][j] = fileBytes[i * maxBytePerPacket + j];
                }
            }

            ret[nbPacket-1] = new byte[fileBytes.Length % maxBytePerPacket];

            for (int i = 0; i < fileBytes.Length % maxBytePerPacket; i++)
            {
                ret[nbPacket - 1][i] = fileBytes[(nbPacket - 1) * maxBytePerPacket + i];
            }


            TCPCopycatPacket[] copycatPacketArray = new TCPCopycatPacket[nbPacket];

            for (int i = 0; i < nbPacket; i++)
            {
                copycatPacketArray[i] = new TCPCopycatPacket(new TCPCopycatPacket.TCPCopycatHeader { dataLenght = ret[i].Length, sequenceNumber = i }, ret[i]);
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
