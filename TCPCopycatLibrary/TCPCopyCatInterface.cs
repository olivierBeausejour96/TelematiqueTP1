using ClientApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp
{
    delegate int UDPListener(UdpClient listener, IPEndPoint groupEp);
    class TCPCopyCatInterface
    {
        private string serverIP = "192.168.12.255";
        UdpClient udpClient;
        IPEndPoint groupEP;
        Socket sending_socket;
        IPAddress send_to_address;
        IPEndPoint sending_end_point;

        public void initialize(int listenPort, bool bLogEnabled)
        {
            serverIP = GetLocalIPAddress();
            udpClient = new UdpClient(listenPort);
            groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


            IPAddress send_to_address = IPAddress.Parse(ip);
            IPEndPoint sending_end_point = new IPEndPoint(send_to_address, port);

        }

        public void startListening()
        {
            // function that listen on port 11000
            UDPListener udpl = delegate (UdpClient listener, IPEndPoint groupEp)
            {
                string received_data;
                byte[] receive_byte_array;
                try
                {
                    while (!done)
                    {
                        // this is the line of code that receives the broadcase message.
                        // It calls the receive function from the object listener (class UdpClient)
                        // It passes to listener the end point groupEP.
                        // It puts the data from the broadcast message into the byte array
                        // named received_byte_array.
                        // I don't know why this uses the class UdpClient and IPEndPoint like this.
                        // Contrast this with the talker code. It does not pass by reference.
                        // Note that this is a synchronous or blocking call.
                        receive_byte_array = listener.Receive(ref groupEP);
                        TCPCopycatPacket packet = TCPCopycatPacket.parse(receive_byte_array);
                        received_data = Encoding.ASCII.GetString(packet.data, 0, packet.header.dataLenght);
                        sendMessageToEndPoint(serverIP, 11001, received_data);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                listener.Close();
                return 0;
            };

            //Task that listen to port 11000
            Task taskA = new Task(() => udpl(udpClient, groupEP));
            taskA.Start();
        }

        public void sendMessageToEndPoint(string ip, int port, TCPCopycatPacket packet)
        {
            Boolean done = false;
            Boolean exception_thrown = false;
            #region comments
            // Create a socket object. This is the fundamental device used to network
            // communications. When creating this object we specify:
            // Internetwork: We use the internet communications protocol
            // Dgram: We use datagrams or broadcast to everyone rather than send to
            // a specific listener
            // UDP: the messages are to be formated as user datagram protocal.
            // The last two seem to be a bit redundant.
            #endregion
            Socket sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
            ProtocolType.Udp);
            #region comments 
            // create an address object and populate it with the IP address that we will use
            // in sending at data to. This particular address ends in 255 meaning we will send
            // to all devices whose address begins with 192.168.2.
            // However, objects of class IPAddress have other properties. In particular, the
            // property AddressFamily. Does this constructor examine the IP address being
            // passed in, determine that this is IPv4 and set the field. If so, the notes
            // in the help file should say so.
            #endregion
            IPAddress send_to_address = IPAddress.Parse(ip);
            #region comments
            // IPEndPoint appears (to me) to be a class defining the first or final data
            // object in the process of sending or receiving a communications packet. It
            // holds the address to send to or receive from and the port to be used. We create
            // this one using the address just built in the previous line, and adding in the
            // port number. As this will be a broadcase message, I don't know what role the
            // port number plays in this.
            #endregion
            IPEndPoint sending_end_point = new IPEndPoint(send_to_address, port);
            #region comments
            // The below three lines of code will not work. They appear to load
            // the variable broadcast_string witha broadcast address. But that
            // address causes an exception when performing the send.
            //
            //string broadcast_string = IPAddress.Broadcast.ToString();
            //Console.WriteLine("broadcast_string contains {0}", broadcast_string);
            //send_to_address = IPAddress.Parse(broadcast_string);
            #endregion

            // the socket object must have an array of bytes to send.
            // this loads the string entered by the user into an array of bytes.
            byte[] send_buffer = packet.toByte();

            // Remind the user of where this is going.
            Console.WriteLine("sending to address: {0} port: {1}",
            sending_end_point.Address,
            sending_end_point.Port);
            try
            {
                sending_socket.SendTo(send_buffer, sending_end_point);
            }
            catch (Exception send_exception)
            {
                exception_thrown = true;
                Console.WriteLine(" Exception {0}", send_exception.Message);
            }
            if (exception_thrown == false)
            {
                Console.WriteLine("Message has been sent to the broadcast address");
            }
            else
            {
                exception_thrown = false;
                Console.WriteLine("The exception indicates the message was not sent.");
            }
        }

        public void initializeClientSession(bool bLogEnabled = true)
        {
            if (bLogEnabled)
            {
                using (var process1 = new Process())
                {
                    process1.StartInfo.FileName = @"U:\Été 2018\IFT585\TP1\ServerSide\LogApp\LogApp\bin\Debug\LogApp.exe";
                    process1.Start();
                }
            }
        }

        public void CloseServer()
        {
            udpClient.Close();
            killAllLogApps();
        }

        public void killAllLogApps()
        {
            foreach (var process in Process.GetProcessesByName(@"LogApp"))
            {
                process.Kill();
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public void doNothing()
        {

        }
    }
}
