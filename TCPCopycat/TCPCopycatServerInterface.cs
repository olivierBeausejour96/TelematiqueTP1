using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace TCPCopycat
{
    public class TCPCopycatServerInterface
    {
        Socket serversocket;
        Socket clientSocket;
        Dictionary<IPEndPoint, Socket> clientSockets;
        List<TCPCopycatPacket> filePacketList;
        HashSet<int> packetReceived;


        public TCPCopycatServerInterface()
        {
            filePacketList = new List<TCPCopycatPacket>();
            packetReceived = new HashSet<int>();
        }

        public void ClientSocketReceivedPacketCallback(TCPCopycatPacket packet, IPEndPoint sender)
        {
            Console.WriteLine("Received Packet numbered: " + packet.header.sequenceNumber.ToString());

            if (!packetReceived.Contains(packet.header.sequenceNumber))
            {
                filePacketList.Add(packet);
                packetReceived.Add(packet.header.sequenceNumber);
            }

            packet.header.acknowledgeNumber = packet.header.sequenceNumber + 1;
            TCPCopyCatController.sendMessageToEndPoint(clientSocket, sender, packet);

            if (packet.header.FIN == 1)
            {
                filePacketList.Sort(delegate (TCPCopycatPacket a, TCPCopycatPacket b) 
                {
                    if (a.header.sequenceNumber < b.header.sequenceNumber)
                        return -1;
                    return 1;
                });
                
                TCPCopycatPacketManager.TCPCopycatPacketArrayToFile(@"C:\Users\beao3002\Desktop\qwe2.zip", filePacketList.ToArray());
            }
        }

        public void ServerReceivedPacketCallback(TCPCopycatPacket packet, IPEndPoint sender)
        {
            Console.WriteLine("Received new connection from " + sender.Address + " port: " + sender.Port);
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint qwe = new IPEndPoint(sender.Address, 0);
            clientSocket.Bind(qwe);
            Console.WriteLine("Client socket listening on port: " + qwe.Port.ToString());
            TCPCopyCatController.startListenOnSocketAsync(clientSocket, ClientSocketReceivedPacketCallback);
            packet.header.acknowledgeNumber = packet.header.sequenceNumber + 1;
            TCPCopyCatController.sendMessageToEndPoint(clientSocket, sender, packet);
        }
        public void initializeServer(int port)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, port);
            serversocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serversocket.Bind(endpoint);
            clientSockets = new Dictionary<IPEndPoint, Socket>();
            Console.WriteLine("Server Initialized");

            TCPCopyCatController.startListenOnSocketAsync(serversocket, ServerReceivedPacketCallback);
        }
        public TCPCopyCatController.responseCode ProcessHandShake(IPEndPoint clientEndpoint)
        {

            return TCPCopyCatController.responseCode.NOT_IMPLEMENTED;
        }

        public TCPCopyCatController.responseCode ProcessCloseConnection(Socket clientSocket)
        {

            return TCPCopyCatController.responseCode.NOT_IMPLEMENTED;
        }
    }
}
