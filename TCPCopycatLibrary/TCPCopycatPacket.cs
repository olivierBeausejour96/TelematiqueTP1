using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    class TCPCopycatPacket
    {
        public struct Header
        {
            public int dataLenght;
            public int packetNumber;
        }

        public Header header;
        public Byte[] data;

        public TCPCopycatPacket(Header h, Byte[] d)
        {
            header = h;
            data = d;
        }

        public TCPCopycatPacket()
        {
        }

        public Byte[] toByte()
        {
            Byte[] binaryPacket = new byte[header.dataLenght + (sizeof(int) + sizeof(int))]; 

            int offset = 0;
            for (int i = 0; i < sizeof(int); ++i)  //converting header.dataLenght
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.dataLenght)[sizeof(int) - 1 - i];
            }
            offset += sizeof(int);

            for (int i = 0; i < sizeof(int); ++i) //converting header.packetNumber
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.packetNumber)[sizeof(int) - 1 - i];
            }
            offset += sizeof(int);

            for (int i = 0; i < header.dataLenght; ++i)
            {
                binaryPacket[i + offset] = data[i];
            }

            return binaryPacket;
        }
        
        public static TCPCopycatPacket parse(Byte[] byteData)
        {
            Header tmp = new Header();
            tmp.dataLenght = 0;

            int offset = 0;
            for (int i = 0; i < sizeof(int); ++i) //parsing header.dataLenght
            {
                tmp.dataLenght = tmp.dataLenght << 8;
                tmp.dataLenght += byteData[i + offset];
            }
            offset += sizeof(int);

            for (int i = 0; i < sizeof(int); ++i) //parsing header.packetNumber
            {
                tmp.packetNumber = tmp.packetNumber << 8;
                tmp.packetNumber += byteData[i + offset];
            }
            offset += sizeof(int);

            Byte[] data = new byte[tmp.dataLenght];

            for (int i = 0; i < data.Length; ++i) //parsing data
            {
                data[i] = byteData[i + offset];
            }

            return new TCPCopycatPacket(tmp, data);
        } 
    }
}
