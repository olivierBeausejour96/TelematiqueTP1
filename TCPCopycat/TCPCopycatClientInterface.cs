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


        public Socket socket;
        private string fileName;
        int windowSize;
        int windowLowerbound;
        int windowHigherbound;
        int sequenceNumberOffset;
        TCPCopycatPacket[] filePackets;
        List<TCPCopycatReceiveMessageCallback> registeredCallbacks;
        public IPEndPoint serverEndpoint;
        private readonly object syncLock = new object();
        private readonly object timerLock = new object();
        int nextPacketInd = 0;

        Dictionary<int, TCPCopycatReceiveMessageCallback> dictionaryRegisteredPacket;
        Dictionary<int, TCPCopyCatController.responseCode> dictionaryRegisteredPacketResponse;
        Dictionary<int, TaskTimer> dictionaryTimer;
        Dictionary<int, TCPCopycatPacket> dictionaryPacket;



        TCPCopycatServerInterface serverInstance;

        

        public TCPCopycatClientInterface()
        {
            registeredCallbacks = new List<TCPCopycatReceiveMessageCallback>();
            dictionaryRegisteredPacket = new Dictionary<int, TCPCopycatReceiveMessageCallback>();
            dictionaryRegisteredPacketResponse = new Dictionary<int, TCPCopyCatController.responseCode>();
            dictionaryTimer = new Dictionary<int, TaskTimer>();
            dictionaryPacket = new Dictionary<int, TCPCopycatPacket>();
        }

        public async Task<TCPCopyCatController.responseCode> connectToServer(IPAddress _IPAddress, int serverPort, int options, string fileToSend = "")
        {
            IPEndPoint serverEndPoint = new IPEndPoint(_IPAddress, serverPort);
            fileName = fileToSend;
            return await connectToServer(serverEndPoint, options);
        }

        public async Task<TCPCopyCatController.responseCode> connectToServer(IPEndPoint serverEndpoint, int options)
        {

            IPEndPoint test = new IPEndPoint(IPAddress.Any, 0);

            this.serverEndpoint = serverEndpoint;

            if ((options & TCPCopycatPacket.OPTION_UPLOAD) != 0)
            {
                const int connectionSequenceNumber = 0;

                if (socket == null)
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                else if (socket.IsBound)
                    return await getPacketResponse(connectionSequenceNumber + 1);
                socket.Bind(test);
                TCPCopyCatController.startListenOnSocketAsync(socket, onPacketReceive);

                TCPCopycatPacket.TCPCopycatHeader header = new TCPCopycatPacket.TCPCopycatHeader();

                header.SYN = 1;
                header.sequenceNumber = connectionSequenceNumber;
                header.ACK = 0;
                header.dataLenght = 0;

                TCPCopycatPacket connectionPacket = new TCPCopycatPacket(header, new byte[1]);
                connectionPacket.setOptions((Int16)(TCPCopycatPacket.OPTION_NONE | TCPCopycatPacket.OPTION_UPLOAD));

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
            else if ((options & TCPCopycatPacket.OPTION_DOWNLOAD) != 0)
            {
                serverInstance = new TCPCopycatServerInterface();

                socket = serverInstance.serversocket;

                const int connectionSequenceNumber = 0;
                if (socket == null)
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                else if (socket.IsBound)
                    return await getPacketResponse(connectionSequenceNumber + 1);
                socket.Bind(test);


                TCPCopycatReceiveMessageCallback receivedMessageCallbackLambda = delegate (TCPCopycatPacket packet, IPEndPoint sender)
                {
                    serverInstance.AddClient(sender, socket);

                    serverInstance.ClientSocketReceivedPacketCallback(packet, sender);

                    return TCPCopyCatController.responseCode.OK;
                };

                TCPCopyCatController.startListenOnSocketAsync(socket, onPacketReceive);

                TCPCopycatPacket.TCPCopycatHeader header = new TCPCopycatPacket.TCPCopycatHeader();

                header.SYN = 1;
                header.sequenceNumber = connectionSequenceNumber;
                header.ACK = 0;

                byte[] fileNameBytes = Encoding.ASCII.GetBytes(fileName);
                header.dataLenght = fileNameBytes.Length;
                TCPCopycatPacket connectionPacket = new TCPCopycatPacket(header, fileNameBytes);
                connectionPacket.setOptions((Int16)(TCPCopycatPacket.OPTION_NONE | TCPCopycatPacket.OPTION_DOWNLOAD));

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
            else
            {
                Console.WriteLine("Unrecognized option");
                throw new Exception("Not a valid option parameter");
            }
        }


        private void registerPacket(TCPCopycatPacket packet, TCPCopycatReceiveMessageCallback lambda, int waitTime)
        {
            if (waitTime < 0)
            {
                throw new Exception("waitTime must be positive");
            }

            //Console.WriteLine("registering packet: " + packet.header.sequenceNumber);
            if (dictionaryRegisteredPacket.ContainsKey(packet.header.sequenceNumber + 1))
            {
                dictionaryRegisteredPacket[packet.header.sequenceNumber + 1] = lambda;
            }
            else
            {
                dictionaryRegisteredPacket.Add(packet.header.sequenceNumber + 1, lambda);
            }
            //Console.WriteLine(dictionaryRegisteredPacket.Count.ToString() + " packets registered");


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
            Console.WriteLine("---Received packet number: " + packet.header.sequenceNumber.ToString());
            if (dictionaryRegisteredPacket.ContainsKey(packet.header.acknowledgeNumber))
            {
                dictionaryRegisteredPacketResponse[packet.header.acknowledgeNumber] = dictionaryRegisteredPacket[packet.header.acknowledgeNumber](packet, sender);
                unregisterPacket(packet.header.acknowledgeNumber);
            }
            else
            {
                //Console.WriteLine("---ClientServerPacketReceived: " + (packet.header.sequenceNumber).ToString());
                if (serverInstance != null)
                {
                    serverInstance.ClientSocketReceivedPacketCallback(packet, sender);
                }
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

        public void sendFile(string filePath, int firstack = 0)
        {
            filePackets = TCPCopycatPacketManager.FileToTCPCopycatPacket(@filePath);
            filePackets[0].header.acknowledgeNumber = firstack;
            windowSize = 10;
            windowLowerbound = 25;
            windowHigherbound = windowLowerbound + windowSize;
            sequenceNumberOffset = 25;
            sendFilePackets(filePackets);
        }


        public void sendFilePackets(TCPCopycatPacket[] filePackets)
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

            packet.header.sequenceNumber += sequenceNumberOffset;

            TCPCopycatReceiveMessageCallback receivedMessageCallbackLambda = delegate (TCPCopycatPacket _packet, IPEndPoint sender)
            {
                unregisterPacket(_packet.header.acknowledgeNumber);
                setPacketResponse(_packet.header.acknowledgeNumber, TCPCopyCatController.responseCode.OK);

                // this ensure we only send 10 files at a time and also respect the selective repeat structure
                if (_packet.header.sequenceNumber < windowLowerbound)
                {
                    Console.WriteLine("************** Already received packet " + (_packet.header.sequenceNumber).ToString() + " ***********");
                }
                else if (_packet.header.sequenceNumber > windowLowerbound)
                {
                    Console.WriteLine("************** Not in order packet received " + (_packet.header.sequenceNumber).ToString() + " ***********");
                }
                else if (_packet.header.sequenceNumber == windowLowerbound)
                {
                    Console.WriteLine("************** In order " + (_packet.header.sequenceNumber).ToString() + " ***********");
                }
                translateWindow();
                

                return TCPCopyCatController.responseCode.OK;
            };

            if (!TCPCopyCatController.sendMessageToEndPoint(socket, serverEndpoint, packet))
            {
                Console.WriteLine("registering file packet" + packet.header.sequenceNumber.ToString());
                registerPacket(packet, receivedMessageCallbackLambda, 2500);
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

        public void translateWindow()
        {
            //Console.WriteLine("WindowLowerBound : " + windowLowerbound.ToString());
            if (dictionaryRegisteredPacketResponse.ContainsKey(windowLowerbound+1) && dictionaryRegisteredPacketResponse[windowLowerbound+1] == TCPCopyCatController.responseCode.OK)
            {
                windowLowerbound++;
                //Console.WriteLine("new WindowLowerBound : " + windowLowerbound.ToString());
                windowHigherbound++;
                sendFilePacket(GetNextPacketToSend());
                translateWindow();
            }
        }
    }
}
