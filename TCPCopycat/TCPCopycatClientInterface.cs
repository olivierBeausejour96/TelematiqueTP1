using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace TCPCopycat
{
    public class TCPCopycatClientInterface
    {
        Socket socket;
        int windowSize;
        TCPCopycatPacket[] filePackets;
        List<TCPCopycatReceiveMessageCallback> registeredCallbacks;
        IPEndPoint serverEndpoint;
        private readonly object syncLock = new object();
        int nextPacketInd = 0;

        Dictionary<int, TCPCopycatReceiveMessageCallback> dictionaryRegisteredPacket;
        Dictionary<int, TCPCopyCatController.responseCode> dictionaryRegisteredPacketResponse;
        Dictionary<int, TaskTimer> dictionaryTimer;
        Dictionary<int, TCPCopycatPacket> dictionaryPacket;

        

        public TCPCopycatClientInterface()
        {
            registeredCallbacks = new List<TCPCopycatReceiveMessageCallback>();
            dictionaryRegisteredPacket = new Dictionary<int, TCPCopycatReceiveMessageCallback>();
            dictionaryRegisteredPacketResponse = new Dictionary<int, TCPCopyCatController.responseCode>();
            dictionaryTimer = new Dictionary<int, TaskTimer>();
            dictionaryPacket = new Dictionary<int, TCPCopycatPacket>();
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
            TCPCopyCatController.startListenOnSocketAsync(socket, onPacketReceive);

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
                    this.serverEndpoint = sender;
                    return TCPCopyCatController.responseCode.OK;
                }
                return TCPCopyCatController.responseCode.UNKNOWN_ERROR;
            };



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

            Console.WriteLine("registering packet: " + packet.header.sequenceNumber);
            if (dictionaryRegisteredPacket.ContainsKey(packet.header.sequenceNumber + 1))
            {
                dictionaryRegisteredPacket[packet.header.sequenceNumber + 1] = lambda;
            }
            else
            {
                dictionaryRegisteredPacket.Add(packet.header.sequenceNumber + 1, lambda);
            }


            setPacketResponse(packet.header.sequenceNumber + 1, TCPCopyCatController.responseCode.NONE);

            ElapsedEventHandler timerCallbackLambda = delegate (object state, System.Timers.ElapsedEventArgs e)
            {
                TaskTimer t = (TaskTimer)state;
                TCPCopycatPacket packetToSend = t.packetToSend;
                TCPCopycatReceiveMessageCallback l = t.lambda;
                int w = t.waitTime;
                t.Dispose();
                if (dictionaryRegisteredPacketResponse[packetToSend.header.sequenceNumber+1] == TCPCopyCatController.responseCode.OK) //unregister timer is called before the timer is even initialized
                    return;
                Console.WriteLine("packet: " + packetToSend.header.sequenceNumber.ToString() + " lost");
                unregisterPacket(packetToSend.header.sequenceNumber + 1);
                setPacketResponse(packetToSend.header.sequenceNumber + 1, TCPCopyCatController.responseCode.NONE);
                TCPCopyCatController.sendMessageToEndPoint(socket, serverEndpoint, packetToSend);
                registerPacket(packetToSend, l, w);
            };
            

            TaskTimer timer = new TaskTimer();

            timer.Interval = waitTime; // set the interval as 10000 ms
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timerCallbackLambda); // set the event to execute
            timer.AutoReset = false; // false: only execute once
            timer.Enabled = true; // to decide if execute the event of timer specified

            timer.packetToSend = packet;
            timer.lambda = lambda;
            timer.waitTime = waitTime;


            registerTimer(packet.header.sequenceNumber + 1, timer);
        }

        private void setPacketResponse(int ack, TCPCopyCatController.responseCode response)
        {
            if (dictionaryRegisteredPacketResponse.ContainsKey(ack))
            {
                dictionaryRegisteredPacketResponse[ack] = response;
            }
            else
            {
                dictionaryRegisteredPacketResponse.Add(ack, response);
            }
        }

        private void registerTimer(int ack, TaskTimer timer)
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

        public void initiateFileTransfer()
        {
            filePackets = TCPCopycatPacketManager.FileToTCPCopycatPacket(@"C:\Users\beao3002\Desktop\qwe.zip");
            windowSize = 5;
            int firstSeqNumber = 25;
            sendFilePackets(windowSize, filePackets, firstSeqNumber);
        }


        public void sendFilePackets(int windowSize, TCPCopycatPacket[] filePackets, int firstSeqNumber)
        {
            int i = 0;
            while ( i < windowSize)
            {
                sendFilePacket(GetNextPacketToSend());
                i++;
            }
        }

        public void sendFilePacket(TCPCopycatPacket packet)
        {
            if (packet == null)
                return;

            if (packet.header.sequenceNumber == filePackets[filePackets.Length-1].header.sequenceNumber)
            {
                packet.header.FIN = 1;
            }

            packet.header.sequenceNumber += 25;

            TCPCopycatReceiveMessageCallback receivedMessageCallbackLambda = delegate (TCPCopycatPacket _packet, IPEndPoint sender)
            {
                unregisterPacket(_packet.header.acknowledgeNumber);
                sendFilePacket(GetNextPacketToSend());
                return TCPCopyCatController.responseCode.OK;
            };

            Console.WriteLine("Sending file to port: " + serverEndpoint.Port.ToString());
            if (!TCPCopyCatController.sendMessageToEndPoint(socket, serverEndpoint, packet))
            {
                registerPacket(packet, receivedMessageCallbackLambda, 1000);
            }
        }

        public TCPCopycatPacket GetNextPacketToSend()
        {
            lock (syncLock)
            {
                ++nextPacketInd;
                if(nextPacketInd > filePackets.Length)
                {
                    return null;
                }
                return filePackets[nextPacketInd-1];
            }
        }


        public TCPCopyCatController.responseCode closeServerConnection()
        {
            return TCPCopyCatController.responseCode.NOT_IMPLEMENTED;
        }
    }
}
