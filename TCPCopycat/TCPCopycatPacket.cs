using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPCopycat
{
    public class TCPCopycatPacket
    {

        private static int MAXIMUM_PACKET_SIZE = 1400;

        public enum TCPCopycatPacketOptions
        {
            NO_OPTION = 0,
            INITIATE_FILE_TRANSFER = 1
        }

        public struct TCPCopycatHeader
        {
            public int dataLenght;          // size of data
            public int sequenceNumber;      // sequenceNumber seen in class
            public int acknowledgeNumber;   // acknowledgeNumber seen in class
            public Int16 windowSize;        // windowSize for SelectiveRepeat
            public Int16 options;           // command that can be communicated (IE. I'm about to send you a file)

            public byte OPT; // true -> Options are valid
            public byte ACK; // true -> acknowledgeNumber is valid
            public byte SYN; // used for handshake to initiate connections
            public byte FIN; // used to close connections
        }

        public TCPCopycatHeader header;
        public Byte[] data;

        public TCPCopycatPacket(TCPCopycatHeader h, Byte[] d)
        {
            header = h;
            data = d;
        }

        public TCPCopycatPacket()
        {
        }

        public Byte[] serialize()
        {
            Byte[] binaryPacket = new byte[header.dataLenght + (
                sizeof(int)     + //TCPCopycatHeader.dataLenght
                sizeof(int)     + //TCPCopycatHeader.sequenceNumber
                sizeof(int)     + //TCPCopycatHeader.acknowledgeNumber
                sizeof(Int16)   + //TCPCopycatHeader.windowSize
                sizeof(Int16)   + //TCPCopycatHeader.options
                sizeof(byte)    + //TCPCopycatHeader.OPT
                sizeof(byte)    + //TCPCopycatHeader.ACK
                sizeof(byte)    + //TCPCopycatHeader.SYN
                sizeof(byte)      //TCPCopycatHeader.FIN
                )];

            if (binaryPacket.Length >= MAXIMUM_PACKET_SIZE)
            {
                throw new Exception("Maximum packet size reached, should not happen");
            }

            #region HeaderSerialization
            int offset = 0;

            /******** header.dataLenght *********/
            for (int i = 0; i < sizeof(int); ++i)
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.dataLenght)[sizeof(int) - 1 - i];
            }
            offset += sizeof(int);

            /******** header.sequenceNumber *********/
            for (int i = 0; i < sizeof(int); ++i) 
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.sequenceNumber)[sizeof(int) - 1 - i];
            }
            offset += sizeof(int);

            /******** header.acknowledgeNumber *********/
            for (int i = 0; i < sizeof(int); ++i)
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.acknowledgeNumber)[sizeof(int) - 1 - i];
            }
            offset += sizeof(int);

            /******** header.windowSize *********/
            for (int i = 0; i < sizeof(Int16); ++i)
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.windowSize)[sizeof(Int16) - 1 - i];
            }
            offset += sizeof(Int16);

            /******** header.options *********/
            for (int i = 0; i < sizeof(Int16); ++i)
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.options)[sizeof(Int16) - 1 - i];
            }
            offset += sizeof(Int16);

            /******** header.OPT *********/
            for (int i = 0; i < sizeof(byte); ++i)
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.OPT)[sizeof(byte) - 1 - i];
            }
            offset += sizeof(byte);

            /******** header.ACK *********/
            for (int i = 0; i < sizeof(byte); ++i)
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.ACK)[sizeof(byte) - 1 - i];
            }
            offset += sizeof(byte);

            /******** header.SYN *********/
            for (int i = 0; i < sizeof(byte); ++i)
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.SYN)[sizeof(byte) - 1 - i];
            }
            offset += sizeof(byte);

            /******** header.FIN *********/
            for (int i = 0; i < sizeof(byte); ++i)
            {
                binaryPacket[i + offset] = BitConverter.GetBytes(header.FIN)[sizeof(byte) - 1 - i];
            }
            offset += sizeof(byte);
            #endregion

            #region DataSerializtion
            for (int i = 0; i < header.dataLenght; ++i)
            {
                binaryPacket[i + offset] = data[i];
            }
            #endregion

            return binaryPacket;
        }
        
        public static TCPCopycatPacket parse(Byte[] byteData)
        {
            if (byteData.Length >= MAXIMUM_PACKET_SIZE)
            {
                throw new Exception("Data lenght must be lower than 1500 byte");
            }

            #region HeaderParsing
            TCPCopycatHeader header = new TCPCopycatHeader();
            int offset = 0;

            /* header.dataLenght ************/
            header.dataLenght = 0;
            for (int i = 0; i < sizeof(int); ++i) 
            {
                header.dataLenght = header.dataLenght << 8;
                header.dataLenght += byteData[i + offset];
            }
            offset += sizeof(int);

            /* header.sequenceNumber ********/
            header.sequenceNumber = 0;
            for (int i = 0; i < sizeof(int); ++i)
            {
                header.sequenceNumber = header.sequenceNumber << 8;
                header.sequenceNumber += byteData[i + offset];
            }
            offset += sizeof(int);

            /* header.acknowledgeNumber ********/
            header.acknowledgeNumber = 0;
            for (int i = 0; i < sizeof(int); ++i)
            {
                header.acknowledgeNumber = header.acknowledgeNumber << 8;
                header.acknowledgeNumber += byteData[i + offset];
            }
            offset += sizeof(int);

            /* header.windowSize ********/
            header.windowSize = 0;
            for (int i = 0; i < sizeof(Int16); ++i)
            {
                header.windowSize = (Int16)(header.windowSize << 8);
                header.windowSize += byteData[i + offset];
            }
            offset += sizeof(Int16);

            /* header.options ********/
            header.options = 0;
            for (int i = 0; i < sizeof(Int16); ++i)
            {
                header.options = (Int16)(header.options << 8);
                header.options += byteData[i + offset];
            }
            offset += sizeof(Int16);

            /* header.OPT ********/
            header.OPT = 0;
            for (int i = 0; i < sizeof(byte); ++i)
            {
                header.OPT = (byte)(header.OPT << 8);
                header.OPT += byteData[i + offset];
            }
            offset += sizeof(byte);

            /* header.ACK ********/
            header.ACK = 0;
            for (int i = 0; i < sizeof(byte); ++i)
            {
                header.ACK = (byte)(header.ACK << 8);
                header.ACK += byteData[i + offset];
            }
            offset += sizeof(byte);

            /* header.SYN ********/
            header.SYN = 0;
            for (int i = 0; i < sizeof(byte); ++i)
            {
                header.SYN = (byte)(header.SYN << 8);
                header.SYN += byteData[i + offset];
            }
            offset += sizeof(byte);

            /* header.FIN ********/
            header.FIN = 0;
            for (int i = 0; i < sizeof(byte); ++i)
            {
                header.FIN = (byte)(header.FIN << 8);
                header.FIN += byteData[i + offset];
            }
            offset += sizeof(byte);
            #endregion

            #region DataParsing
            Byte[] data = new byte[header.dataLenght];

            for (int i = 0; i < data.Length; ++i) //parsing data
            {
                data[i] = byteData[i + offset];
            }
            #endregion

            return new TCPCopycatPacket(header, data);
        }

        public override string ToString()
        {
        //     public int dataLenght;          // size of data
        //public int sequenceNumber;      // sequenceNumber seen in class
        //public int acknowledgeNumber;   // acknowledgeNumber seen in class
        //public Int16 windowSize;        // windowSize for SelectiveRepeat
        //public Int16 options;           // command that can be communicated (IE. I'm about to send you a file)

        //public byte OPT; // true -> Options are valid
        //public byte ACK; // true -> acknowledgeNumber is valid
        //public byte SYN; // used for handshake to initiate connections
        //public byte FIN; // used to close connections

        string tcpPacketStringValue = "Header: \n" +
                    "  dataLenght: " + header.dataLenght + "\n" +
                    "  sequenceNumber: " + header.sequenceNumber + "\n" +
                    "  acknowledgeNumber: " + header.acknowledgeNumber + "\n" +
                    "  windowSize: " + header.windowSize + "\n" +
                    "  options: " + header.options + "\n" +
                    "  OPT: " + header.OPT + "\n" +
                    "  ACK: " + header.ACK + "\n" +
                    "  SYN: " + header.SYN + "\n" +
                    "  FIN: " + header.FIN + "\n\n" +
                    "Data: \n";
            for (int i = 0; i < data.Length; i++)
            {
                tcpPacketStringValue += "  byte" + i + ": " + data[i] + "\n";
            }
            return tcpPacketStringValue;
        }
    }
}
