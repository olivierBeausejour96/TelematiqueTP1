using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPCopycat
{
    delegate TCPCopyCatController.responseCode SendMessageLambda(Socket socket, IPEndPoint endpoint, TCPCopycatPacket message, int waitTime, int nbRetry, bool bLog);
    delegate void UDPListener();
    public delegate void ReceivedPacketCallback(TCPCopycatPacket packet, IPEndPoint sender);
    delegate TCPCopyCatController.responseCode TimedListen();
    delegate void TimerCallbackLambda(object state);
    public delegate TCPCopyCatController.responseCode TCPCopycatReceiveMessageCallback(TCPCopycatPacket responsePacket, IPEndPoint sender);
    public abstract class TCPCopyCatController
    {

        public enum responseCode
        {
            NONE = 0,
            OK = 200,
            BAD_REQUEST = 400,
            NO_RESPONSE = 444,
            NOT_IMPLEMENTED = 501,
            UNKNOWN_ERROR = 520
        }

        private TCPCopyCatController() { }

        public responseCode serverProcessHandshake(IPEndPoint clientEndPoint)
        {
            throw new NotImplementedException("LOL NOOB");
        }

        public static void startListenOnSocketAsync(Socket socket, ReceivedPacketCallback callbackLambda)
        {
            UDPListener udpl = delegate ()
            {
                try
                {
                    while (true)
                    {
                        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                        EndPoint Remote = (EndPoint)sender;
                        byte[] receive_byte_array = new byte[1399];
                        socket.ReceiveFrom(receive_byte_array, ref Remote);
                        sender = (IPEndPoint)Remote;

                        callbackLambda(TCPCopycatPacket.parse(receive_byte_array), sender);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            };
            Task taskA = new Task(() => udpl());
            taskA.Start();
        }

        public void sendMessageToEndPoint(Socket socket, IPEndPoint receiver, TCPCopycatPacket packet, TCPCopycatReceiveMessageCallback callbackLambda, int timeoutTime = 1000)
        {
            sendMessageToEndPoint(socket, receiver, packet);
        }

        public static bool sendMessageToEndPoint(Socket socket, IPEndPoint receiver, TCPCopycatPacket packet)
        {
            Boolean exception_thrown = false;

            try
            {
                socket.SendTo(packet.serialize(), receiver);
            }
            catch (Exception send_exception)
            {
                exception_thrown = true;
                Console.WriteLine(" Exception {0}", send_exception.Message);
            }
            if (exception_thrown == true)
            {
                exception_thrown = false;
                Console.WriteLine("The exception indicates the message was not sent.");
            }
            return exception_thrown;
        }

        public void CloseSocket()
        {
        }

        public void doNothing()
        {

        }

        /// <summary>
        /// to be tested
        /// </summary>
        /// <param name="task"></param>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        public responseCode executeTimedRequest(Task<responseCode> task, int waitTime)
        {
            responseCode ret;
            TimerCallbackLambda timerCallbackLambda = delegate (object state)
            {
                Timer t = (Timer)state;
                t.Dispose();
                task.Dispose();
            };

            if (waitTime < 0)
            {
                throw new Exception("waitTime must be positive");
            }
            
            if (task.Status == TaskStatus.Created && task.Status != TaskStatus.Running)
            {
                task.Start();
                Timer timer = new Timer(new TimerCallback(timerCallbackLambda));
                timer.Change(waitTime, 0);
  
                
                try
                {
                    task.Wait();
                    ret = responseCode.OK;
                }
                catch (ObjectDisposedException)
                {
                    ret = responseCode.NO_RESPONSE;
                }
                catch (Exception)
                {
                    ret = responseCode.UNKNOWN_ERROR;
                }
            }
            else
            {
                throw new Exception("Task invalid already Started");
            }
            return ret;
        }
    }
}
