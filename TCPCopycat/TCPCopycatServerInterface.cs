using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace TCPCopycat
{
    public class TCPCopycatServerInterface
    {
        Socket serversocket;
        Dictionary<IPEndPoint, Socket> clientSockets;


        public TCPCopycatServerInterface()
        {

        }

        public void ClientSocketReceivedPacketCallback(TCPCopycatPacket packet, IPEndPoint sender)
        {
            Console.WriteLine("Received Packet numbered: " + packet.header.sequenceNumber.ToString());

            packet.header.acknowledgeNumber = packet.header.sequenceNumber + 1;
        }

        public void ServerReceivedPacketCallback(TCPCopycatPacket packet, IPEndPoint sender)
        {
            Console.WriteLine("Received new connection from " + sender.Address + " port: " + sender.Port);
            Console.WriteLine("Loopback: " + IPAddress.Loopback.ToString() + " Any: " + IPAddress.Any.ToString());
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint qwe = new IPEndPoint(IPAddress.Any, sender.Port);
            clientSocket.Bind(qwe);
            clientSockets.Add(sender, clientSocket);
            Console.WriteLine("holla");
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
