using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPCopycat
{
    delegate void UDPListener(Socket socket);
    public class TCPCopyCatInterface
    {
        Socket socket;

        public void initialize(int listenPort)
        {
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(groupEP);
            startListening();
        }

        public void startListening()
        {
            UDPListener udpl = delegate (Socket socket)
            {
                try
                {
                    while (true)
                    {
                        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                        EndPoint Remote = (EndPoint)sender;
                        byte[] receive_byte_array = new byte[2048];
                        socket.ReceiveFrom(receive_byte_array, ref Remote);
                        sender = (IPEndPoint)Remote;

                        onPacketReceive(sender, receive_byte_array);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            };

            Task taskA = new Task(() => udpl(socket));
            taskA.Start();
        }

        public void onPacketReceive(IPEndPoint sender, byte[] data)
        {
            //dostuff
        }

        public void sendMessageToEndPoint(IPEndPoint receiver, byte[] packet)
        {
            Boolean done = false;
            Boolean exception_thrown = false;

            // Remind the user of where this is going.
            Console.WriteLine("sending to address: {0} port: {1}",
            receiver.Address,
            receiver.Port);
            try
            {
                socket.SendTo(packet, receiver);
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

        public void CloseServer()
        {
            socket.Disconnect(false);
            socket.Close();
        }

        public void doNothing()
        {

        }
    }
}
