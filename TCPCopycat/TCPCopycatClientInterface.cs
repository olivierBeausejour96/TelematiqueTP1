using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPCopycat
{
    public class TCPCopycatClientInterface
    {
        Socket socket;
        List<TCPCopycatReceiveMessageCallback> registeredCallbacks;
        IPEndPoint serverEndpoint;

        Dictionary<int, TCPCopycatReceiveMessageCallback> registeredPacket;
        Dictionary<int, TCPCopyCatController.responseCode> registeredPacketResponse;


        public TCPCopycatClientInterface()
        {
            registeredCallbacks = new List<TCPCopycatReceiveMessageCallback>();
            registeredPacket = new Dictionary<int, TCPCopycatReceiveMessageCallback>();
            registeredPacketResponse = new Dictionary<int, TCPCopyCatController.responseCode>();
        }

        public async Task<TCPCopyCatController.responseCode> connectToServer(int serverPort)
        {
            if (socket.IsBound)
                throw new Exception("Already Connected to another Server");
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
            return await connectToServer(serverEndPoint);
        }

        public async Task<TCPCopyCatController.responseCode> connectToServer(IPEndPoint serverEndpoint)
        {
            const int connectionSequenceNumber = 0;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(serverEndpoint);

            this.serverEndpoint = serverEndpoint;

            TCPCopycatPacket.TCPCopycatHeader header = new TCPCopycatPacket.TCPCopycatHeader();

            header.SYN = 1;
            header.sequenceNumber = connectionSequenceNumber;
            header.ACK = 0;
            header.dataLenght = 0;

            TCPCopycatPacket connectionPacket = new TCPCopycatPacket(header, new byte[0]);

            TCPCopycatReceiveMessageCallback receivedMessageCallbackLambda = delegate (TCPCopycatPacket packet, IPEndPoint sender)
            {
                if (packet.header.SYN == 1 && packet.header.acknowledgeNumber == (connectionSequenceNumber + 1))
                    return TCPCopyCatController.responseCode.OK;
                return TCPCopyCatController.responseCode.UNKNOWN_ERROR;
            };


            TCPCopyCatController.startListenOnSocketAsync(socket, onPacketReceive);
            if (TCPCopyCatController.sendMessageToEndPoint(socket, serverEndpoint, connectionPacket))
            {
                registerPacket(connectionPacket, receivedMessageCallbackLambda, 1000);
                return await getPacketResponse(connectionSequenceNumber + 1);
            }
            else
            {
                return TCPCopyCatController.responseCode.BAD_REQUEST;
            }
        }

        private void registerPacket(TCPCopycatPacket packet, TCPCopycatReceiveMessageCallback lambda, int waitTime)
        {
            if (waitTime < 0)
            {
                throw new Exception("waitTime must be positive");
            }

            registeredPacket.Add(packet.header.sequenceNumber + 1, lambda);
            registeredPacketResponse.Add(packet.header.sequenceNumber + 1, TCPCopyCatController.responseCode.NONE);

            TimerCallbackLambda timerCallbackLambda = delegate (object state)
            {
                Timer t = (Timer)state;
                t.Dispose();
                unregisterPacket(packet.header.sequenceNumber + 1);
                registeredPacketResponse[packet.header.sequenceNumber + 1] = TCPCopyCatController.responseCode.NO_RESPONSE;
            };

            Timer timer = new Timer(new TimerCallback(timerCallbackLambda));
            timer.Change(waitTime, 0);
        }

        private void unregisterPacket(int packetNumber)
        {
            registeredPacket.Remove(packetNumber);
        }

        public void onPacketReceive(TCPCopycatPacket packet, IPEndPoint sender)
        {
            if (registeredPacket.ContainsKey(packet.header.acknowledgeNumber))
            {
                registeredPacketResponse.Add(packet.header.acknowledgeNumber, registeredPacket[packet.header.acknowledgeNumber](packet, sender));
                unregisterPacket(packet.header.acknowledgeNumber);
            }
        }

        delegate TCPCopyCatController.responseCode testDelegate();
        public async Task<TCPCopyCatController.responseCode> getPacketResponse(int packetAcknowledgeNumber)
        {
            testDelegate qwe = delegate ()
            {
                while (registeredPacket.ContainsKey(packetAcknowledgeNumber))
                {
                    Thread.Sleep(1);
                }
                if (registeredPacketResponse.ContainsKey(packetAcknowledgeNumber))
                {
                    return registeredPacketResponse[packetAcknowledgeNumber];
                }
                return TCPCopyCatController.responseCode.NONE;
            };
            Task<TCPCopyCatController.responseCode> task = new Task<TCPCopyCatController.responseCode>(() => qwe());
            task.Start();
            return await task;
        }

        public TCPCopyCatController.responseCode initiateFileTransfer()
        {
            return TCPCopyCatController.responseCode.NOT_IMPLEMENTED;
        }

        public TCPCopyCatController.responseCode closeServerConnection()
        {
            return TCPCopyCatController.responseCode.NOT_IMPLEMENTED;
        }
    }
}
