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

        Dictionary<int, TCPCopycatReceiveMessageCallback> dictionaryRegisteredPacket;
        Dictionary<int, TCPCopyCatController.responseCode> dictionaryRegisteredPacketResponse;
        Dictionary<int, Timer> dictionaryTimer;

        public TCPCopycatClientInterface()
        {
            registeredCallbacks = new List<TCPCopycatReceiveMessageCallback>();
            dictionaryRegisteredPacket = new Dictionary<int, TCPCopycatReceiveMessageCallback>();
            dictionaryRegisteredPacketResponse = new Dictionary<int, TCPCopyCatController.responseCode>();
            dictionaryTimer = new Dictionary<int, Timer>();
        }

        public async Task<TCPCopyCatController.responseCode> connectToServer(IPAddress _IPAddress, int serverPort)
        {
            IPEndPoint serverEndPoint = new IPEndPoint(_IPAddress, serverPort);
            return await connectToServer(serverEndPoint);
        }

        public async Task<TCPCopyCatController.responseCode> connectToServer(IPEndPoint serverEndpoint)
        {
            const int connectionSequenceNumber = 0;

            if (socket == null)
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            else if (socket.IsBound)
                throw new Exception("Already connected to another server!");

            IPEndPoint test = new IPEndPoint(IPAddress.Loopback, 0);
            socket.Bind(test);

            this.serverEndpoint = serverEndpoint;

            TCPCopycatPacket.TCPCopycatHeader header = new TCPCopycatPacket.TCPCopycatHeader();

            header.SYN = 1;
            header.sequenceNumber = connectionSequenceNumber;
            header.ACK = 0;
            header.dataLenght = 0;

            TCPCopycatPacket connectionPacket = new TCPCopycatPacket(header, new byte[1]);

            TCPCopycatReceiveMessageCallback receivedMessageCallbackLambda = delegate (TCPCopycatPacket packet, IPEndPoint sender)
            {
                if (packet.header.SYN == 1 && packet.header.acknowledgeNumber == (connectionSequenceNumber + 1))
                {
                    return TCPCopyCatController.responseCode.OK;
                }
                return TCPCopyCatController.responseCode.UNKNOWN_ERROR;
            };


            TCPCopyCatController.startListenOnSocketAsync(socket, onPacketReceive);
            if (!TCPCopyCatController.sendMessageToEndPoint(socket, serverEndpoint, connectionPacket))
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

            dictionaryRegisteredPacket.Add(packet.header.sequenceNumber + 1, lambda);
            dictionaryRegisteredPacketResponse.Add(packet.header.sequenceNumber + 1, TCPCopyCatController.responseCode.NONE);

            TimerCallbackLambda timerCallbackLambda = delegate (object state)
            {
                Console.WriteLine("in timer");
                Timer t = (Timer)state;
                t.Dispose();
                unregisterPacket(packet.header.sequenceNumber + 1);
                dictionaryRegisteredPacketResponse[packet.header.sequenceNumber + 1] = TCPCopyCatController.responseCode.NO_RESPONSE;
            };


            Timer timer = new Timer(new TimerCallback(timerCallbackLambda));
            timer.Change(waitTime, 0);

            registerTimer(packet.header.sequenceNumber + 1, timer);
        }

        private void registerTimer(int ack, Timer timer)
        {
            if (!dictionaryTimer.ContainsKey(ack))
                dictionaryTimer.Add(ack, timer);
        }

        private void unregisterTimer(int ack)
        {
            if (dictionaryTimer.ContainsKey(ack))
            {
                dictionaryTimer[ack].Dispose();
            }
        }

        private void unregisterPacket(int packetNumber)
        {
            dictionaryRegisteredPacket.Remove(packetNumber);
            unregisterTimer(packetNumber);
        }

        public void onPacketReceive(TCPCopycatPacket packet, IPEndPoint sender)
        {
            if (dictionaryRegisteredPacket.ContainsKey(packet.header.acknowledgeNumber))
            {
                Console.WriteLine("in onPacketReceive");
                dictionaryRegisteredPacketResponse[packet.header.acknowledgeNumber] = dictionaryRegisteredPacket[packet.header.acknowledgeNumber](packet, sender);
                unregisterPacket(packet.header.acknowledgeNumber);
                Console.WriteLine("Response: " + dictionaryRegisteredPacketResponse[packet.header.acknowledgeNumber].ToString());
            }
        }

        delegate TCPCopyCatController.responseCode testDelegate();
        public async Task<TCPCopyCatController.responseCode> getPacketResponse(int packetAcknowledgeNumber)
        {
            testDelegate qwe = delegate ()
            {
                while (dictionaryRegisteredPacket.ContainsKey(packetAcknowledgeNumber))
                {
                    Thread.Sleep(1);
                }
                if (dictionaryRegisteredPacketResponse.ContainsKey(packetAcknowledgeNumber))
                {
                    return dictionaryRegisteredPacketResponse[packetAcknowledgeNumber];
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
