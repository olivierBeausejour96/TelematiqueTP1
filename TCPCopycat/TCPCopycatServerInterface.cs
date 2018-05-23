using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPCopycat
{
    public class TCPCopycatServerInterface
    {

        Socket serversocket;
        Dictionary<IPEndPoint, Socket> clientSockets;
        Dictionary<Socket, List<TCPCopycatPacket>> filePacketList;
        Dictionary<Socket, HashSet<int>> packetReceived;



        public TCPCopycatServerInterface()
        {
            filePacketList = new Dictionary<Socket, List<TCPCopycatPacket>>();
            packetReceived = new Dictionary<Socket, HashSet<int>>();
        }

        public void ClientSocketReceivedPacketCallback(TCPCopycatPacket packet, IPEndPoint sender)
        {
            Console.WriteLine("Received Packet numbered: " + packet.header.sequenceNumber.ToString());

            if (!packetReceived[GetClientSocketFromEndpoint(sender)].Contains(packet.header.sequenceNumber))
            {
                if(packet.header.FIN == 1)
                {
                    Console.WriteLine("Packet FIN received");
                }
                filePacketList[GetClientSocketFromEndpoint(sender)].Add(packet);
                packetReceived[GetClientSocketFromEndpoint(sender)].Add(packet.header.sequenceNumber);
            }

            packet.header.acknowledgeNumber = packet.header.sequenceNumber + 1;
            TCPCopyCatController.sendMessageToEndPoint(GetClientSocketFromEndpoint(sender), sender, packet);
            /*
            if (packet.header.acknowledgeNumber == 30)
            {
                Console.WriteLine("********** SENDING MULTIPLE TIME SAME PACKET *************");
                latency(250, 1251);
                TCPCopyCatController.sendMessageToEndPoint(GetClientSocketFromEndpoint(sender), sender, packet);
                latency(250, 1251);
                TCPCopyCatController.sendMessageToEndPoint(GetClientSocketFromEndpoint(sender), sender, packet);
                latency(250, 1251);
                TCPCopyCatController.sendMessageToEndPoint(GetClientSocketFromEndpoint(sender), sender, packet);
            }*/
            /*
            if (packet.header.acknowledgeNumber != 28)
            {
                TCPCopyCatController.sendMessageToEndPoint(GetClientSocketFromEndpoint(sender), sender, packet);
            }
            if (packet.header.acknowledgeNumber == 31)
            {
                Console.WriteLine("*** GoingTo send packet 30 ack ***");
                Thread.Sleep(10000);
                packet.header.acknowledgeNumber = 28;
                TCPCopyCatController.sendMessageToEndPoint(GetClientSocketFromEndpoint(sender), sender, packet);
                Console.WriteLine("*** SENT PACKET ***");
            }
            */
            if (packet.header.FIN == 1)
            {
                filePacketList[GetClientSocketFromEndpoint(sender)].Sort(delegate (TCPCopycatPacket a, TCPCopycatPacket b) 
                {
                    if (a.header.sequenceNumber < b.header.sequenceNumber)
                        return -1;
                    return 1;
                });
                
                TCPCopycatPacketManager.TCPCopycatPacketArrayToFile(@"./" + sender.Port.ToString() , filePacketList[GetClientSocketFromEndpoint(sender)].ToArray());
            }
        }

        public void ServerReceivedPacketCallback(TCPCopycatPacket packet, IPEndPoint sender)
        {
            Console.WriteLine("Received new connection from " + sender.Address + " port: " + sender.Port);

            clientSockets.Add(sender, new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));
            IPEndPoint qwe = new IPEndPoint(IPAddress.Any, 0);
            clientSockets[sender].Bind(qwe);
            packetReceived.Add(GetClientSocketFromEndpoint(sender), new HashSet<int>());
            filePacketList.Add(GetClientSocketFromEndpoint(sender), new List<TCPCopycatPacket>());
            Console.WriteLine("Client socket listening on port: " + sender.Port.ToString());
            TCPCopyCatController.startListenOnSocketAsync(GetClientSocketFromEndpoint(sender), ClientSocketReceivedPacketCallback);
            packet.header.acknowledgeNumber = packet.header.sequenceNumber + 1;
            TCPCopyCatController.sendMessageToEndPoint(GetClientSocketFromEndpoint(sender), sender, packet);
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

        public Socket GetClientSocketFromEndpoint(IPEndPoint endpoint)
        {
            if (clientSockets.ContainsKey(endpoint))
            {
                return clientSockets[endpoint];
            }
            return null;
        }

        public TCPCopyCatController.responseCode ProcessHandShake(IPEndPoint clientEndpoint)
        {
            return TCPCopyCatController.responseCode.NOT_IMPLEMENTED;
        }

        public TCPCopyCatController.responseCode ProcessCloseConnection(Socket clientSocket)
        {
            return TCPCopyCatController.responseCode.NOT_IMPLEMENTED;
        }

        public void latency(int min, int max)
        {
            Random rnd = new Random(123);
            int rndNb = rnd.Next(min, max);
            Thread.Sleep(rndNb);
        }
    }
}
